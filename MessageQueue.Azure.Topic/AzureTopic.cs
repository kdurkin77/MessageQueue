using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Azure.Topic
{
    public sealed class AzureTopic<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly AzureTopicOptions<TMessage> _options;
        private readonly IMessageFormatter<TMessage> _formatter;
        private readonly TopicClient _topicClient;

        private static readonly MessageAttributes _emptyAttributes = new MessageAttributes();

        public AzureTopic(ILogger<AzureTopic<TMessage>> logger, IOptions<AzureTopicOptions<TMessage>> options, IMessageFormatter<TMessage> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            var builder = new ServiceBusConnectionStringBuilder()
            {
                Endpoint = _options.Endpoint,
                EntityPath = _options.EntityPath,
                SasKey = _options.SharedAccessKey,
                SasKeyName = _options.SharedAccessKeyName,
                TransportType = _options.TransportType
            };

            _topicClient = new TopicClient(builder);
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

            var messageBytes = _formatter.MessageToBytes(message);

            var topicMessage = new Message(messageBytes)
            {
                ContentType = attributes.ContentType,
                Label = attributes.Label
            };

            if (attributes.UserProperties != null)
            {
                foreach (var userProperty in attributes.UserProperties)
                {
                    topicMessage.UserProperties.Add(userProperty.Key, userProperty.Value);
                }
            }

            _logger.LogTrace($"posting to {_topicClient.Path}/{topicMessage.Label}");

            await _topicClient.SendAsync(topicMessage).ConfigureAwait(false);
        }

        public Task<IMessageReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AzureTopic<TMessage>));
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
                _topicClient.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            _disposed = true;
        }

        ~AzureTopic() => Dispose(false);

#if !NETSTANDARD2_0

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _topicClient.CloseAsync().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

#endif

    }
}
