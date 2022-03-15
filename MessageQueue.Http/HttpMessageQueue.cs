using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;

#if NET5_0_OR_GREATER
using System.Collections.Generic;
#endif

namespace KM.MessageQueue.Http
{
    public sealed class HttpMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        internal readonly HttpMessageQueueOptions<TMessage> _options;
        internal readonly HttpClient _client;

        private static readonly MessageAttributes _emptyAttributes = new();

        public HttpMessageQueue(ILogger<HttpMessageQueue<TMessage>> logger, IOptions<HttpMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _client = new HttpClient();
            if(_options.Headers is not null)
            {
                foreach (var header in _options.Headers)
                {
                    _client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            };
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

            var url = _options.Url;
            if(_options.ShouldUseQueryParameters)
            {
                var queryDict = _options.QueryMessageFormatter.FormatMessage(message);
                url = QueryHelpers.AddQueryString(_options.Url, queryDict);
            }

            var request = new HttpRequestMessage(_options.Method, url);
            if(_options.ShouldUseBody)
            {
                request.Content = _options.BodyMessageFormatter.FormatMessage(message);
                if (attributes.ContentType is not null)
                {
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(attributes.ContentType);
                }
            }

            if (attributes.UserProperties is not null)
            {
                foreach (var property in attributes.UserProperties)
                {
#if NET5_0_OR_GREATER
                    request.Options.TryAdd(property.Key, property.Value);
#else
                    request.Properties.Add(property);
#endif
                }
            }

            if (attributes.Label is not null)
            {
#if NET5_0_OR_GREATER
                request.Options.TryAdd("Label", attributes.Label);
#else
                request.Properties.Add("Label", attributes.Label);
#endif
            }

            var result = await _client.SendAsync(request, cancellationToken);
            _options.CheckHttpResponse(result);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpMessageQueue<TMessage>));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _client.Dispose();

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

            _client.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif
    }
}
