using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    internal sealed class AzureTopicMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        private readonly AzureTopicMessageQueue<TMessage> _queue;
        private readonly string? _subscriptionName;
        private readonly object? _userData;
        private readonly ServiceBusReceiver _serviceBusReceiver;
        private readonly int _readCount;

        public AzureTopicMessageQueueReader(ILogger logger, AzureTopicMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _subscriptionName = options.SubscriptionName;
            _userData = options.UserData;

            var receiverOptions = new ServiceBusReceiverOptions()
            {
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                PrefetchCount = options.PrefetchCount ?? 1,
            };

            _serviceBusReceiver = _queue._serviceBusClient.CreateReceiver(_queue._entityPath, options.SubscriptionName, receiverOptions);

            _readCount = options.ReadCount ?? 1;
            if (_readCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadCount));
            }

            Name = options.Name ?? nameof(AzureTopicMessageQueueReader<TMessage>);

            _logger.LogTrace($"{Name} initialized");
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
                var azureMessage = await _serviceBusReceiver.ReceiveMessageAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                var attributes = new MessageAttributes()
                {
                    ContentType = azureMessage.ContentType,
                    Label = _subscriptionName,
                    UserProperties = azureMessage.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };

                var originalMessage = await _queue._messageFormatter.RevertMessage(azureMessage.Body.ToArray()).ConfigureAwait(false);

                var (completionResult, result) = await action(originalMessage, attributes, _userData, cancellationToken).ConfigureAwait(false);
                switch (completionResult)
                {
                    case CompletionResult.Complete:
                        await _serviceBusReceiver.CompleteMessageAsync(azureMessage, cancellationToken).ConfigureAwait(false);
                        break;

                    case CompletionResult.Abandon:
                        await _serviceBusReceiver.AbandonMessageAsync(azureMessage, null, cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        throw new NotSupportedException($"{result}");
                }

                return (completionResult, result);
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

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var azureMessages = await _serviceBusReceiver.ReceiveMessagesAsync(_readCount, cancellationToken: cancellationToken).ConfigureAwait(false);
                var items = new List<(TMessage message, MessageAttributes atts)>();
                foreach (var azureMessage in azureMessages)
                {
                    var attributes = new MessageAttributes()
                    {
                        ContentType = azureMessage.ContentType,
                        Label = _subscriptionName,
                        UserProperties = azureMessage.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };

                    var originalMessage = await _queue._messageFormatter.RevertMessage(azureMessage.Body.ToArray()).ConfigureAwait(false);
                    items.Add((originalMessage, attributes));
                }

                var (completionResult, result) = await action(items, _userData, cancellationToken).ConfigureAwait(false);
                switch (completionResult)
                {
                    case CompletionResult.Complete:
                        foreach (var message in azureMessages)
                        {
                            await _serviceBusReceiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case CompletionResult.Abandon:
                        foreach (var message in azureMessages)
                        {
                            await _serviceBusReceiver.AbandonMessageAsync(message, null, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    default:
                        throw new NotSupportedException($"{result}");
                }

                return (completionResult, result);
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
