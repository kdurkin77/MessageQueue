using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt.Tcp
{
    internal sealed class TcpMqttReader<TMessage> : IMessageReader<TMessage>
    {
        private bool _disposed = false;
        private string _subscriptionName = string.Empty;

        private readonly TcpMqtt<TMessage> _queue;
        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        public MessageReaderState State { get; private set; } = MessageReaderState.Stopped;

        private CancellationTokenSource? _readerTokenSource = null;
        private CancellationTokenSource ReaderTokenSource
        {
            get => _readerTokenSource ?? throw new SystemException($"{nameof(TcpMqttReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
            set => _readerTokenSource = value;
        }

        public TcpMqttReader(TcpMqtt<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public async Task StartAsync(MessageReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (startOptions is null)
            {
                throw new ArgumentNullException(nameof(startOptions));
            }

            if (startOptions.SubscriptionName is null)
            {
                throw new ArgumentNullException(nameof(startOptions.SubscriptionName));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageReaderState.Running)
                {
                    throw new InvalidOperationException($"{nameof(TcpMqttReader<TMessage>)} is already started");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(TcpMqttReader<TMessage>)} is stopping");
                }

                ReaderTokenSource = new CancellationTokenSource();
                _subscriptionName = startOptions.SubscriptionName;
                await _queue._managedMqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(_subscriptionName).Build());
                _queue._managedMqttClient.UseApplicationMessageReceivedHandler(MessageHandler);

                State = MessageReaderState.Running;
            }
            finally
            {
                _sync.Release();
            }

            async Task MessageHandler(MqttApplicationMessageReceivedEventArgs e)
            {
                if (ReaderTokenSource.IsCancellationRequested)
                {
                    // do not stop the reader in the middle of a read
                    return;
                }

                var attributes = new MessageAttributes()
                {
                    ContentType = e.ApplicationMessage.ContentType,
                    Label = e.ApplicationMessage.Topic,
                    UserProperties = e.ApplicationMessage.UserProperties?.ToDictionary(p => p.Name, p => (object)p.Value)
                };
                await startOptions.MessageHandler.HandleMessageAsync(_queue._formatter, e.ApplicationMessage.Payload, attributes, startOptions.UserData, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageReaderState.Stopped)
                {
                    throw new InvalidOperationException($"{nameof(TcpMqttReader<TMessage>)} is already stopped");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(TcpMqttReader<TMessage>)} is already stopping");
                }

                await InternalStopAsync().ConfigureAwait(false);
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task InternalStopAsync()
        {
            _readerTokenSource?.Cancel();

            await _queue._managedMqttClient.UnsubscribeAsync(_subscriptionName);

            _readerTokenSource?.Dispose();
            State = MessageReaderState.Stopped;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpMqttReader<TMessage>));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                InternalStopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            _disposed = true;
        }

        ~TcpMqttReader() => Dispose(false);

#if !NETSTANDARD2_0

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await InternalStopAsync().ConfigureAwait(false);

            Dispose(false);
            GC.SuppressFinalize(this);
        }

#endif
    }
}
