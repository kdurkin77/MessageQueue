using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    internal sealed class AzureTopicMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        private bool _disposed = false;
        private readonly AzureTopicMessageQueue<TMessage> _queue;

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        public MessageQueueReaderState State { get; private set; } = MessageQueueReaderState.Stopped;

        private ServiceBusProcessor? _serviceBusProcessor = null;
        private ServiceBusProcessor ServiceBusProcessor
        {
            get => _serviceBusProcessor ?? throw new SystemException($"{nameof(AzureTopicMessageQueueReader<TMessage>)}.{nameof(_serviceBusProcessor)} is null");
            set => _serviceBusProcessor = value;
        }

        private CancellationTokenSource? _readerTokenSource = null;
        private CancellationTokenSource ReaderTokenSource
        {
            get => _readerTokenSource ?? throw new SystemException($"{nameof(AzureTopicMessageQueueReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
            set => _readerTokenSource = value;
        }

        public AzureTopicMessageQueueReader(AzureTopicMessageQueue<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public async Task StartAsync(MessageQueueReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (startOptions is null)
            {
                throw new ArgumentNullException(nameof(startOptions));
            }

            if (startOptions.SubscriptionName is null)
            {
                throw new ArgumentException($"{nameof(startOptions)}.{nameof(startOptions.SubscriptionName)} cannot be null");
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageQueueReaderState.Running)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicMessageQueueReader<TMessage>)} is already started");
                }

                if (State == MessageQueueReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicMessageQueueReader<TMessage>)} is stopping");
                }

                ReaderTokenSource = new CancellationTokenSource();

                var opts = new ServiceBusProcessorOptions()
                {
                    ReceiveMode = ServiceBusReceiveMode.PeekLock,
                    AutoCompleteMessages = false,
                    MaxConcurrentCalls = 1
                };

                if (startOptions.PrefetchCount.HasValue)
                {
                    opts.PrefetchCount = startOptions.PrefetchCount.Value;
                }

                ServiceBusProcessor = _queue._serviceBusClient.CreateProcessor(_queue._options.EntityPath, startOptions.SubscriptionName, opts);
                ServiceBusProcessor.ProcessMessageAsync += MessageHandler;
                ServiceBusProcessor.ProcessErrorAsync += ErrorHandler;
                await ServiceBusProcessor.StartProcessingAsync();

                State = MessageQueueReaderState.Running;
            }
            finally
            {
                _sync.Release();
            }

            async Task MessageHandler(ProcessMessageEventArgs args)
            {
                if (ReaderTokenSource.IsCancellationRequested)
                {
                    // do not stop the reader in the middle of a read
                    return;
                }

                var attributes = new MessageAttributes()
                {
                    ContentType = args.Message.ContentType,
                    Label = startOptions.SubscriptionName,
                    UserProperties = args.Message.ApplicationProperties.ToDictionary(a => a.Key, a => a.Value)
                };

                var result = await startOptions.MessageHandler.HandleMessageAsync(_queue._formatter, args.Message.Body.ToArray(), attributes, startOptions.UserData, cancellationToken).ConfigureAwait(false);
                switch (result)
                {
                    case CompletionResult.Complete:
                        await args.CompleteMessageAsync(args.Message, cancellationToken).ConfigureAwait(false);
                        break;

                    case CompletionResult.Abandon:
                        await args.AbandonMessageAsync(args.Message, null, cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException($"{result}");
                }
            }

            async Task ErrorHandler(ProcessErrorEventArgs exceptionReceivedEventArgs)
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
                if (State == MessageQueueReaderState.Stopped)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicMessageQueueReader<TMessage>)} is already stopped");
                }

                if (State == MessageQueueReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(AzureTopicMessageQueueReader<TMessage>)} is already stopping");
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

            var processor = _serviceBusProcessor;
            if (processor != null)
            {
                if (!processor.IsClosed)
                {
                    await processor.CloseAsync().ConfigureAwait(false);
                }
            }

            _readerTokenSource?.Dispose();
            State = MessageQueueReaderState.Stopped;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AzureTopicMessageQueueReader<TMessage>));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            InternalStopAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await InternalStopAsync().ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#endif
    }
}
