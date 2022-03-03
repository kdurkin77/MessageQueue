using Azure.Messaging.ServiceBus;
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
        internal readonly AzureTopicMessageQueueOptions _options;
        internal readonly IMessageFormatter<TMessage, byte[]> _formatter;
        internal readonly ServiceBusClient _serviceBusClient;

        private static readonly MessageAttributes _emptyAttributes = new();

        public AzureTopicMessageQueue(ILogger<AzureTopicMessageQueue<TMessage>> logger, IOptions<AzureTopicMessageQueueOptions> options, IMessageFormatter<TMessage, byte[]> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            var connectionString = $"Endpoint={_options.Endpoint};SharedAccessKeyName={_options.SharedAccessKeyName};SharedAccessKey={_options.SharedAccessKey};EntityPath={_options.EntityPath}";
            var clientOpts = new ServiceBusClientOptions();
            if (_options.TransportType.HasValue)
            {
                clientOpts.TransportType = _options.TransportType.Value;
            }
            _serviceBusClient = new ServiceBusClient(connectionString, clientOpts);
        }

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

            var messageBytes = _formatter.FormatMessage(message);

            var sender = _serviceBusClient.CreateSender(_options.EntityPath);
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
            await sender.DisposeAsync().ConfigureAwait(false);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new AzureTopicMessageQueueReader<TMessage>(this);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AzureTopicMessageQueue<TMessage>));
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
