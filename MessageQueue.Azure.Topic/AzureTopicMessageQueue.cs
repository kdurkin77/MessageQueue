using Azure.Messaging.ServiceBus;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    public sealed class AzureTopicMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public AzureTopicMessageQueue(ILogger<AzureTopicMessageQueue<TMessage>> logger, IOptions<AzureTopicMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _entityPath = opts.EntityPath;
            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
            _serviceBusClient = new ServiceBusClient(opts.ConnectionString, opts.ServiceBusClientOptions);

            Name = opts.Name ?? nameof(AzureTopicMessageQueue<TMessage>);

            MaxReadCount = opts.MaxReadCount ?? 1;
            if (MaxReadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opts.MaxReadCount));
            }

            MaxWriteCount = opts.MaxWriteCount ?? 1;
            if (MaxWriteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opts.MaxWriteCount));
            }

            _logger.LogTrace($"{Name} initialized");
        }


        private bool _disposed = false;

        private readonly ILogger _logger;
        internal string? _entityPath;
        internal readonly IMessageFormatter<TMessage, byte[]> _messageFormatter;
        internal readonly ServiceBusClient _serviceBusClient;

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

        public int MaxWriteCount { get; }
        public int MaxReadCount { get; }

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

        public async Task PostManyMessagesAsync(IEnumerable<(TMessage message, MessageAttributes attributes)> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (!messages.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(messages));
            }

            if (messages.Count() > MaxWriteCount)
            {
                _logger.LogError($"{Name} {nameof(PostManyMessagesAsync)} message count exceeds max write count of {MaxWriteCount}");
                throw new InvalidOperationException($"Message count exceeds max write count of {MaxWriteCount}");
            }

            var azureMessages = new List<ServiceBusMessage>();
            foreach (var (message, attributes) in messages)
            {
                var messageBytes = await _messageFormatter.FormatMessage(message).ConfigureAwait(false);
                var sbMessage = new ServiceBusMessage(messageBytes)
                {
                    ContentType = attributes.ContentType,
                    Subject = attributes.Label
                };

                if (attributes.UserProperties is { } userProperties)
                {
                    foreach (var userProperty in userProperties)
                    {
                        sbMessage.ApplicationProperties.Add(userProperty.Key, userProperty.Value);
                    }
                }

                azureMessages.Add(sbMessage);
            }

            _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} posting {{messageCount}} messages to {{Path}}", azureMessages.Count, $"{_serviceBusClient.FullyQualifiedNamespace}/{_entityPath}");

            await using var sender = _serviceBusClient.CreateSender(_entityPath);
            await sender.SendMessagesAsync(azureMessages, cancellationToken).ConfigureAwait(false);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            var reader = new AzureTopicMessageQueueReader<TMessage>(_logger, this, options);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _serviceBusClient.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _serviceBusClient.DisposeAsync().ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#endif
    }
}
