using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Database.ElasticSearch
{
    public sealed class ElasticSearchMessageQueue<TMessageIn> : IMessageQueue<TMessageIn>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly ElasticSearchMessageQueueOptions _options;
        private readonly IMessageFormatter<TMessageIn, JObject> _formatter;
        private readonly ElasticClient _client;

        private static readonly MessageAttributes _emptyAttributes = new();

        public ElasticSearchMessageQueue(ILogger<ElasticSearchMessageQueue<TMessageIn>> logger, IOptions<ElasticSearchMessageQueueOptions> options, IMessageFormatter<TMessageIn, JObject> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            if(_options.ConnectionSettings is null)
            {
                throw new ArgumentException($"{nameof(_options)}.{nameof(_options.ConnectionSettings)} cannot be null");
            }

            _client = new ElasticClient(_options.ConnectionSettings);

            _logger.LogTrace($"{nameof(ElasticSearchMessageQueue<TMessageIn>)} initialized");
        }

        public Task PostMessageAsync(TMessageIn message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return PostMessageAsync(message, _emptyAttributes, cancellationToken);
        }

        public async Task PostMessageAsync(TMessageIn message, MessageAttributes attributes, CancellationToken cancellationToken)
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

            var messageObject = _formatter.FormatMessage(message);
            var elasticSearchMessage = new ElasticSearchMessage(attributes, messageObject);
            var elasticSearchMessageJson = JsonConvert.SerializeObject(elasticSearchMessage);

            if (string.IsNullOrWhiteSpace(attributes.Label))
            {
                if (string.IsNullOrWhiteSpace(_client.ConnectionSettings.DefaultIndex))
                {
                    throw new Exception("Default index not specified");
                }

                await _client.LowLevel.IndexAsync<StringResponse>(_client.ConnectionSettings.DefaultIndex, (PostData)elasticSearchMessageJson, null, cancellationToken);
            }
            else
            {
                await _client.LowLevel.IndexAsync<StringResponse>(attributes.Label, (PostData)elasticSearchMessageJson, null, cancellationToken);
            }
        }

        public Task<IMessageQueueReader<TMessageIn>> GetReaderAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ElasticSearchMessageQueue<TMessageIn>));
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
