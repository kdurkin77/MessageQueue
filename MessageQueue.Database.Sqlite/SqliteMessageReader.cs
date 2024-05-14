using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Database.Sqlite
{
    internal sealed class SqliteMessageReader<TMessage> : IMessageQueueReader<TMessage>
    {
        public SqliteMessageReader(ILogger logger, SqliteMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _subscriptionName = options.SubscriptionName;
            _userData = options.UserData;
            _readCount = options.ReadCount ?? 1;
            if (_readCount <= 0 || _readCount > queue.MaxReadCount)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadCount), $"{options.ReadCount} must be greater than 0 and cannot be greater than {queue.MaxReadCount}");
            }

            Name = options.Name ?? nameof(SqliteMessageReader<TMessage>);

            _logger.LogTrace($"{Name} initialized");
        }

        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        private readonly SqliteMessageQueue<TMessage> _queue;
        private readonly string? _subscriptionName;
        private readonly object? _userData;
        private readonly int _readCount;


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

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _queue.InternalReadMessageAsync(Wrapper, 1, _userData, cancellationToken).ConfigureAwait(false);
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

            async Task<(CompletionResult CompletionResult, TResult)> Wrapper(IEnumerable<(TMessage message, MessageAttributes attributes)> messages, object? userData, CancellationToken cancellationToken)
            {
                var (message, attr) = messages.Single();
                return await action(message, attr, userData, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<CompletionResult> ReadManyMessagesAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var (completionResult, _) = await ReadManyMessagesAsync(Wrapper, cancellationToken).ConfigureAwait(false);

            return completionResult;


            async Task<(CompletionResult CompletionResult, int)> Wrapper(IEnumerable<(TMessage, MessageAttributes)> messages, object? userData, CancellationToken cancellationToken)
            {
                var completionResult = await action(messages, userData, cancellationToken).ConfigureAwait(false);
                return (completionResult, 0);
            }
        }

        public async Task<(CompletionResult CompletionResult, TResult Result)> ReadManyMessagesAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await _queue.InternalReadMessageAsync(action, _readCount, _userData, cancellationToken).ConfigureAwait(false);
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

        public async Task<(bool, CompletionResult)> TryReadManyMessagesAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var completionResult = await ReadManyMessagesAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult);
            }
            catch (OperationCanceledException)
            {
                return (false, default);
            }
        }

        public async Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryReadManyMessagesAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var (completionResult, result) = await ReadManyMessagesAsync(action, cancellationToken).ConfigureAwait(false);
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
