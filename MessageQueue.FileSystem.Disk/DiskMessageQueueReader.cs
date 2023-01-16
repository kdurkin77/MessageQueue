using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        private readonly DiskMessageQueue<TMessage> _queue;
        private readonly object? _userData;

        public DiskMessageQueueReader(ILogger logger, DiskMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _userData = options.UserData;


            Name = options.Name ?? nameof(DiskMessageQueueReader<TMessage>);
        }


        public string Name { get; }

        public async Task<CompletionResult> ReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var (completionResult, _) = await ReadMessageAsync(Wrapper, cancellationToken).ConfigureAwait(false);

            return completionResult;


            async Task<(CompletionResult CompletionResult, int)> Wrapper(TMessage message, MessageAttributes attributes, object? userData, CancellationToken cancellationToken)
            {
                var completionResult = await action(message, attributes, userData, cancellationToken).ConfigureAwait(false);
                return (completionResult, 0);
            }
        }

        public async Task<(CompletionResult CompletionResult, TResult Result)> ReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _queue.InternalReadMessageAsync(action, _userData, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{Name} exception in {nameof(ReadMessageAsync)}");
                throw;
            }
            finally
            {
                _ = _sync.Release();
            }
        }

        public async Task<(bool, CompletionResult)> TryReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var completionResult = await ReadMessageAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult);
            }
            catch (OperationCanceledException)
            {
                return (false, default);
            }
        }

        public async Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var (completionResult, result) = await ReadMessageAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult, result);
            }
            catch (OperationCanceledException)
            {
                return (false, default, default!);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"{_queue.Name} {Name}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

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

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
