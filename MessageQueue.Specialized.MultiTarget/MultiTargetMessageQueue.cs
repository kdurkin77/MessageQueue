using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KM.MessageQueue.Specialized.MultiTarget
{
    public sealed class MultiTargetMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public MultiTargetMessageQueue(ILogger<MultiTargetMessageQueue<TMessage>> logger, IOptions<MultiTargetMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            var targets = opts._targets.ToList().AsReadOnly();
            if (!targets.Any())
            {
                throw new ArgumentException($"{nameof(MultiTargetMessageQueue<TMessage>)} requires at least one target", nameof(options));
            }

            _targets = targets;
            _onUnhandledMessage = opts.OnUnhandledMessage ?? (_ => throw new InvalidOperationException("Message was not handled by any targets"));

            Name = opts.Name ?? nameof(MultiTargetMessageQueue<TMessage>);

            _logger.LogTrace($"{Name} initialized");
        }

        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly IEnumerable<(IMessageQueue<TMessage> MessageQueue, Func<IMessageQueue<TMessage>, TMessage, Task<bool>> Predicate)> _targets;
        private readonly Action<TMessage> _onUnhandledMessage;

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

        public int MaxWriteCount { get; } = 1;
        public int MaxReadCount { get; } = 0;

        public async Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await PostManyMessagesAsync([message], cancellationToken);
        }

        public async Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            await PostManyMessagesAsync([(message, attributes)], cancellationToken);
        }

        public async Task PostManyMessagesAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var messagesWithAtts = messages.Select(message => (message, _emptyAttributes));
            await PostManyMessagesAsync(messagesWithAtts, cancellationToken);
        }

        public async Task PostManyMessagesAsync(IEnumerable<(TMessage, MessageAttributes)> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (messages.Count() > MaxWriteCount)
            {
                throw new InvalidOperationException($"Message count exceeds max write count of {MaxWriteCount}");
            }

            var handled = false;
            var (message, attributes) = messages.First();
            foreach (var (messageQueue, predicate) in _targets)
            {
                if (await predicate(messageQueue, message))
                {
                    _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} posting to {{Label}}, Message: {{Message}}", attributes.Label, message);
                    await messageQueue.PostManyMessagesAsync(messages, cancellationToken).ConfigureAwait(false);
                    handled = true;
                }
            }

            if (!handled)
            {
                _onUnhandledMessage(message);
            }
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MultiTargetMessageQueue<TMessage>));
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

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }
    }
}
