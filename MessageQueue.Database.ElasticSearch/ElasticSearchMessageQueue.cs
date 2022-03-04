using Elasticsearch.Net;
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
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly ElasticSearchMessageQueueOptions<TMessage> _options;
        private readonly ElasticClient _client;

        private static readonly MessageAttributes _emptyAttributes = new();

        public ElasticSearchMessageQueue(ILogger<ElasticSearchMessageQueue<TMessage>> logger, IOptions<ElasticSearchMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if(_options.ConnectionSettings is null)
            {
                throw new ArgumentException($"{nameof(_options)}.{nameof(_options.ConnectionSettings)} cannot be null");
            }

            _client = new ElasticClient(_options.ConnectionSettings);

            _logger.LogTrace($"{nameof(ElasticSearchMessageQueue<TMessage>)} initialized");
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

            ThrowIfDisposed();

            var messageObject = _options.MessageFormatter.FormatMessage(message);
            var elasticSearchMessage = JObject.FromObject(new ElasticSearchMessage(attributes));
            elasticSearchMessage.Merge(messageObject);
            var elasticSearchMessageJson = JsonConvert.SerializeObject(elasticSearchMessage);

            _logger.LogTrace($"posting to {nameof(ElasticSearchMessageQueue<TMessage>)} - {attributes.Label}");

            if (string.IsNullOrWhiteSpace(attributes.Label))
            {
                if (string.IsNullOrWhiteSpace(_client.ConnectionSettings.DefaultIndex))
                {
                    throw new Exception("Default index not specified");
                }

                await _client.LowLevel.IndexAsync<StringResponse>(_client.ConnectionSettings.DefaultIndex, (PostData)elasticSearchMessageJson, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _client.LowLevel.IndexAsync<StringResponse>(attributes.Label, (PostData)elasticSearchMessageJson, null, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ElasticSearchMessageQueue<TMessage>));
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

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif
    }
}
