using Elasticsearch.Net;
using KM.MessageQueue.Formatters.ObjectToJsonObject;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Database.ElasticSearch
{
    public sealed class ElasticSearchMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public ElasticSearchMessageQueue(ILogger<ElasticSearchMessageQueue<TMessage>> logger, IOptions<ElasticSearchMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonObjectFormatter<TMessage>();
            if (opts.ConnectionSettings is null)
            {
                throw new ArgumentException($"{nameof(opts.ConnectionSettings)} is required", nameof(options));
            }

            _client = new ElasticClient(opts.ConnectionSettings);
            Name = opts.Name ?? nameof(ElasticSearchMessageQueue<TMessage>);

            _logger.LogTrace($"{Name} initialized");
        }


        private bool _disposed = false;

        private readonly ILogger _logger;
        private readonly ElasticClient _client;
        private readonly IMessageFormatter<TMessage, JObject> _messageFormatter;

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

        public async Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await PostMessageAsync(message, _emptyAttributes, cancellationToken);
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

            var messageObject = await _messageFormatter.FormatMessage(message).ConfigureAwait(false);
            var elasticSearchMessage = JObject.FromObject(new ElasticSearchMessage(attributes));
            elasticSearchMessage.Merge(messageObject);
            var elasticSearchMessageJson = JsonConvert.SerializeObject(elasticSearchMessage);


            StringResponse? response;
            if (!string.IsNullOrWhiteSpace(attributes.Label))
            {
                _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} posting to {{Label}}, Message: {{Message}}", attributes.Label, elasticSearchMessageJson);
                response = await _client.LowLevel.IndexAsync<StringResponse>(attributes.Label, (PostData)elasticSearchMessageJson, null, cancellationToken).ConfigureAwait(false);
            }
            else if (!string.IsNullOrWhiteSpace(_client.ConnectionSettings.DefaultIndex))
            {
                _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} posting to {{Index}}, Label: {{Label}}, Message: {{Message}}", _client.ConnectionSettings.DefaultIndex, attributes.Label, elasticSearchMessageJson);
                response = await _client.LowLevel.IndexAsync<StringResponse>(_client.ConnectionSettings.DefaultIndex, (PostData)elasticSearchMessageJson, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError($"{Name} {nameof(PostMessageAsync)} label or default index is required");
                throw new Exception("Label or default index is required");
            }

            if (response.Success)
            {
                _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} success response: {{Response}}", response.Body);
            }
            else
            {
                _logger.LogError($"{Name} {nameof(PostMessageAsync)} error response: {{Response}}", response.Body);
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
                throw new ObjectDisposedException(Name);
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
