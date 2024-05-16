using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class ForwarderMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public ForwarderMessageQueue(ILogger<ForwarderMessageQueue<TMessage>> logger, IOptions<ForwarderMessageQueueOptions> options, IMessageQueue<TMessage> sourceQueue, IMessageQueue<TMessage> destinationQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _sourceQueue = sourceQueue ?? throw new ArgumentNullException(nameof(sourceQueue));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));

            if (ReferenceEquals(_sourceQueue, _destinationQueue))
            {
                throw new ArgumentException($"{nameof(sourceQueue)} and {nameof(destinationQueue)} may not be the same");
            }

            _retryDelay = opts.RetryDelay ?? TimeSpan.FromMilliseconds(100);
            _subscriptionName = opts.SourceSubscriptionName;
            _userData = opts.SourceUserData;
            _forwardingErrorHandler = opts.ForwardingErrorHandler;
            _disposeSourceQueue = opts.DisposeSourceQueue ?? false;
            _disposeDestinationQueue = opts.DisposeDestinationQueue ?? false;

            Name = opts.Name ?? nameof(ForwarderMessageQueue<TMessage>);

            //whichever is lesser is our limit since we write whatever we read from the source queue to the destination queue
            MaxReadCount = _sourceQueue.MaxReadCount < _destinationQueue.MaxWriteCount ? _sourceQueue.MaxReadCount : _destinationQueue.MaxWriteCount;
            MaxWriteCount = _sourceQueue.MaxWriteCount;

            _readerLoopTask = Task.Run(ReadSourceQueueLoop);

            _logger.LogTrace($"{Name} initialized");
        }


        private bool _disposed = false;
        private readonly CancellationTokenSource _cancellationSource = new();

        private readonly ILogger _logger;
        private readonly IMessageQueue<TMessage> _sourceQueue;
        private readonly IMessageQueue<TMessage> _destinationQueue;
        private readonly TimeSpan _retryDelay;
        private readonly string? _subscriptionName;
        private readonly object? _userData;
        private readonly Func<Exception, Task<CompletionResult>>? _forwardingErrorHandler;
        private readonly bool _disposeSourceQueue;
        private readonly bool _disposeDestinationQueue;

        private readonly Task _readerLoopTask;


        private async Task ReadSourceQueueLoop()
        {
            _logger.LogTrace($"{Name} entering {nameof(ReadSourceQueueLoop)}");

            try
            {
                var readerOptions = new MessageQueueReaderOptions<TMessage>()
                {
                    SubscriptionName = _subscriptionName,
                    UserData = _userData,
                    ReadCount = MaxReadCount
                };

                var sourceReader = await _sourceQueue.GetReaderAsync(readerOptions, _cancellationSource.Token).ConfigureAwait(false);

                while (!_cancellationSource.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogTrace($"{Name} {nameof(ReadSourceQueueLoop)} invoking {nameof(sourceReader.ReadManyMessagesAsync)}");
                        var result = await sourceReader.ReadManyMessagesAsync(PushToDestinationQueue, _cancellationSource.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException) when (_cancellationSource.IsCancellationRequested)
                    {
                        // cancellation requested
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{Name} exception in {nameof(ReadSourceQueueLoop)} in {nameof(sourceReader.ReadManyMessagesAsync)}.  Retry in {{RetryDelay}}", _retryDelay);
                        await Task.Delay(_retryDelay).ConfigureAwait(false);
                    }
                }

                _logger.LogTrace($"{Name} {nameof(ReadSourceQueueLoop)} cancellation requested");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Name} exception in {nameof(ReadSourceQueueLoop)}");
                throw;
            }

            _logger.LogTrace($"{Name} {nameof(ReadSourceQueueLoop)} exiting");

            async Task<CompletionResult> PushToDestinationQueue(IEnumerable<(TMessage message, MessageAttributes attributes)> messages, object? userData, CancellationToken cancellationToken)
            {
                try
                {
                    await _destinationQueue.PostManyMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
                    return CompletionResult.Complete;
                }
                catch (Exception ex)
                {
                    if (_forwardingErrorHandler is not { } errorHandler)
                    {
                        _logger.LogError(ex, $"{Name} exception in {nameof(ReadSourceQueueLoop)} in {nameof(PushToDestinationQueue)}");
                        throw;
                    }
                    else
                    {
                        _logger.LogTrace(ex, $"{Name} exception in {nameof(ReadSourceQueueLoop)} in {nameof(PushToDestinationQueue)}.  Invoking user error handler");
                        try
                        {
                            var completionResult = await errorHandler(ex);

                            _logger.LogTrace($"{Name} {nameof(ReadSourceQueueLoop)} in {nameof(PushToDestinationQueue)} user error handler returned {{CompletionResult}}", completionResult);
                            return completionResult;
                        }
                        catch (Exception handlerEx)
                        {
                            _logger.LogError(handlerEx, $"{Name} exception in {nameof(ReadSourceQueueLoop)} in {nameof(PushToDestinationQueue)} in user error handler");
                            throw;
                        }
                    }
                }
            }
        }


        public string Name { get; }

        public int MaxWriteCount { get; }
        public int MaxReadCount { get; }


        public async Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _logger.LogTrace($"{Name} invoking {nameof(PostMessageAsync)}(2)");
            await _sourceQueue.PostMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }

        public async Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _logger.LogTrace($"{Name} invoking {nameof(PostMessageAsync)}(3)");
            await _sourceQueue.PostMessageAsync(message, attributes, cancellationToken).ConfigureAwait(false);
        }

        public async Task PostManyMessagesAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _logger.LogTrace($"{Name} invoking {nameof(PostManyMessagesAsync)}");
            await _sourceQueue.PostManyMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
        }

        public async Task PostManyMessagesAsync(IEnumerable<(TMessage message, MessageAttributes attributes)> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _logger.LogTrace($"{Name} invoking {nameof(PostManyMessagesAsync)}");
            await _sourceQueue.PostManyMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            _logger.LogTrace($"{Name} invoking {nameof(GetReaderAsync)}");
            return await _destinationQueue.GetReaderAsync(options, cancellationToken).ConfigureAwait(false);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        private async Task ShutdownAsync()
        {
            _cancellationSource.Cancel();

#if NETSTANDARD2_1_OR_GREATER || NET

            if (_disposeSourceQueue)
            {
                await _sourceQueue.DisposeAsync().ConfigureAwait(false);
            }

            if (_disposeDestinationQueue)
            {
                await _destinationQueue.DisposeAsync().ConfigureAwait(false);
            }

#else

            if (_disposeSourceQueue)
            {
                _sourceQueue.Dispose();
            }

            if (_disposeDestinationQueue)
            {
                _destinationQueue.Dispose();
            }

            await Task.CompletedTask;
#endif
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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

            await ShutdownAsync().ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#endif
    }
}
