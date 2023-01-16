using Azure.Messaging.ServiceBus;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    public sealed class AzureTopicMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;

        private readonly ILogger _logger;
        internal readonly AzureTopicMessageQueueOptions<TMessage> _options;
        internal readonly IMessageFormatter<TMessage, byte[]> _messageFormatter;
        internal readonly ServiceBusClient _serviceBusClient;

        private static readonly MessageAttributes _emptyAttributes = new();

        public AzureTopicMessageQueue(ILogger<AzureTopicMessageQueue<TMessage>> logger, IOptions<AzureTopicMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _serviceBusClient = new ServiceBusClient(_options.ConnectionString, _options.ServiceBusClientOptions);
            _messageFormatter = _options.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
            Name = nameof(AzureTopicMessageQueue<TMessage>);
        }

        public string Name { get; }

        public Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return PostMessageAsync(message, _emptyAttributes, cancellationToken);
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

            var messageBytes = await _messageFormatter.FormatMessage(message).ConfigureAwait(false);
            await using var sender = _serviceBusClient.CreateSender(_options.EntityPath);
            var sbMessage = new ServiceBusMessage(messageBytes)
            {
                ContentType = attributes.ContentType,
                Subject = attributes.Label
            };

            if (attributes.UserProperties != null)
            {
                foreach (var userProperty in attributes.UserProperties)
                {
                    sbMessage.ApplicationProperties.Add(userProperty.Key, userProperty.Value);
                }
            }

            _logger.LogTrace($"posting to {_serviceBusClient.FullyQualifiedNamespace}/{_options.EntityPath}/{attributes.Label}");

            await sender.SendMessageAsync(sbMessage, cancellationToken).ConfigureAwait(false);
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
