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
        private readonly string _subscriptionName;
        private readonly int? _prefectCount;

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
            _subscriptionName = queue._options.SubscriptionName ?? throw new ArgumentNullException(nameof(queue._options.SubscriptionName));
            _prefectCount = queue._options.PrefetchCount;
        }

        public Task StartAsync(IMessageHandler<TMessage> messageHandler, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            return StartAsync(messageHandler, null, cancellationToken);
        }

        public async Task StartAsync(IMessageHandler<TMessage> messageHandler, object? userData, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
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

                SubscriptionClient = new SubscriptionClient(_queue._topicClient.ServiceBusConnection, _topicPath, _subscriptionName, ReceiveMode.PeekLock, RetryPolicy.Default);

                if (_prefectCount.HasValue)
                {
                    SubscriptionClient.PrefetchCount = _prefectCount.Value;
                }

                var handlerOptions = new MessageHandlerOptions(ExceptionHandler(messageHandler, userData))
                {
                    AutoComplete = false,
                    MaxConcurrentCalls = 1
                };

                SubscriptionClient.RegisterMessageHandler(MessageHandler(messageHandler, userData), handlerOptions);

                State = MessageReaderState.Running;
            }
            finally
            {
                _sync.Release();
            }
        }

        private Func<Message, CancellationToken, Task> MessageHandler(IMessageHandler<TMessage> messageHandler, object? userData)
        {
            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            return async (topicMessage, cancellationToken) =>
            {
                if (ReaderTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var message = _queue._formatter.BytesToMessage(topicMessage.Body);
                var attributes = new MessageAttributes()
                {
                    ContentType = topicMessage.ContentType,
                    Label = topicMessage.Label,
                    UserProperties = topicMessage.UserProperties
                };

                var result = await messageHandler.HandleMessageAsync(message, attributes, userData, cancellationToken).ConfigureAwait(false);
                if (result == CompletionResult.Complete)
                {
                    await SubscriptionClient.CompleteAsync(topicMessage.SystemProperties.LockToken).ConfigureAwait(false);
                }
            };
        }

        private Func<ExceptionReceivedEventArgs, Task> ExceptionHandler(IMessageHandler<TMessage> messageHandler, object? userData)
        {
            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            return (exceptionReceivedEventArgs) =>
            {
                return messageHandler.HandleErrorAsync(exceptionReceivedEventArgs.Exception, userData, ReaderTokenSource.Token);
            };
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
