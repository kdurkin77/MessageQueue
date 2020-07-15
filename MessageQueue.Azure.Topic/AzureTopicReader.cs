using Microsoft.Azure.ServiceBus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    internal sealed class AzureTopicReader<TMessage> : IMessageReader<TMessage>
    {
        private bool _disposed = false;
        private readonly AzureTopic<TMessage> _queue;
        private readonly string _topicPath;
        private readonly int? _prefetchCount;

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        public MessageReaderState State { get; private set; } = MessageReaderState.Stopped;

        private ISubscriptionClient? _subscriptionClient = null;
        private ISubscriptionClient SubscriptionClient
        {
            get => _subscriptionClient ?? throw new SystemException($"{nameof(AzureTopicReader<TMessage>)}.{nameof(_subscriptionClient)} is null");
            set => _subscriptionClient = value;
        }

        private CancellationTokenSource? _readerTokenSource = null;
        private CancellationTokenSource ReaderTokenSource
        {
            get => _readerTokenSource ?? throw new SystemException($"{nameof(AzureTopic<TMessage>)}.{nameof(_readerTokenSource)} is null");
            set => _readerTokenSource = value;
        }

        public AzureTopicReader(AzureTopic<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _topicPath = queue._options.EntityPath ?? throw new ArgumentNullException(nameof(queue._options.EntityPath));
            _prefetchCount = queue._options.PrefetchCount;
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
                    throw new InvalidOperationException($"{nameof(AzureTopicReader<TMessage>)} is already started");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicReader<TMessage>)} is stopping");
                }

                ReaderTokenSource = new CancellationTokenSource();

                SubscriptionClient = new SubscriptionClient(_queue._topicClient.ServiceBusConnection, _topicPath, startOptions.SubscriptionName, ReceiveMode.PeekLock, RetryPolicy.Default);

                if (_prefetchCount.HasValue)
                {
                    SubscriptionClient.PrefetchCount = _prefetchCount.Value;
                }

                var handlerOptions = new MessageHandlerOptions(ExceptionHandler)
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 1
                };

                SubscriptionClient.RegisterMessageHandler(MessageHandler, handlerOptions);

                State = MessageReaderState.Running;
            }
            finally
            {
                _sync.Release();
            }

            async Task MessageHandler(Message topicMessage, CancellationToken cancellationToken)
            {
                if (ReaderTokenSource.IsCancellationRequested)
                {
                    // do not stop the reader in the middle of a read
                    return;
                }

                var message = _queue._formatter.BytesToMessage(topicMessage.Body);
                var attributes = new MessageAttributes()
                {
                    ContentType = topicMessage.ContentType,
                    Label = topicMessage.Label,
                    UserProperties = topicMessage.UserProperties
                };

                var result = await startOptions.MessageHandler.HandleMessageAsync(message, attributes, startOptions.UserData, cancellationToken).ConfigureAwait(false);
                switch (result)
                {
                    case CompletionResult.Complete:
                        await SubscriptionClient.CompleteAsync(topicMessage.SystemProperties.LockToken).ConfigureAwait(false);
                        break;

                    case CompletionResult.Abandon:
                        await SubscriptionClient.AbandonAsync(topicMessage.SystemProperties.LockToken).ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException($"{result}");
                }
            }

            async Task ExceptionHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
            {
                await startOptions.MessageHandler.HandleErrorAsync(exceptionReceivedEventArgs.Exception, startOptions.UserData, ReaderTokenSource.Token).ConfigureAwait(false);
                await InternalStopAsync().ConfigureAwait(false);
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
                    throw new InvalidOperationException($"{nameof(AzureTopicReader<TMessage>)} is already stopped");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicReader<TMessage>)} is already stopping");
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

            var client = _subscriptionClient;
            if (client != null)
            {
                if (!client.IsClosedOrClosing)
                {
                    await client.CloseAsync().ConfigureAwait(false);
                }
            }

            _readerTokenSource?.Dispose();
            State = MessageReaderState.Stopped;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AzureTopicReader<TMessage>));
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

        ~AzureTopicReader() => Dispose(false);

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
