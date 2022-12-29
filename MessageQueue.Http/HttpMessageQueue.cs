using KM.MessageQueue.Formatters.JsonStringToDictionary;
using KM.MessageQueue.Formatters.ObjectToJsonObject;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Http
{
    public sealed class HttpMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        internal readonly HttpClient _client;
        internal readonly Uri _uri;
        internal readonly HttpMethod _method;
        internal readonly bool _shouldUseBody;
        internal readonly bool _shouldUseQueryParameters;
        internal readonly Action<HttpResponseMessage?> _checkHttpResponse;
        internal readonly Func<HttpRequestMessage, Task>? _beforeSendMessage;
        internal readonly IMessageFormatter<TMessage, HttpContent> _bodyMessageFormatter;
        internal readonly IMessageFormatter<TMessage, IDictionary<string, string>> _queryMessageFormatter;

        private static readonly MessageAttributes _emptyAttributes = new();

        public HttpMessageQueue(ILogger<HttpMessageQueue<TMessage>> logger, IOptions<HttpMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _uri = opts.Uri ?? throw new ArgumentException($"{nameof(opts)}.{nameof(opts.Uri)} cannot be null");
            _method = opts.Method ?? HttpMethod.Get;
            _shouldUseBody = opts.ShouldUseBody ?? false;
            _shouldUseQueryParameters = opts.ShouldUseQueryParameters ?? !_shouldUseBody;
            _checkHttpResponse = opts.CheckHttpResponse ??
                (message =>
                {
                    if (message is null)
                    {
                        throw new ArgumentNullException(nameof(message));
                    }
                    message.EnsureSuccessStatusCode();
                });
            _beforeSendMessage = opts.BeforeSendMessage;
            _bodyMessageFormatter = opts.BodyMessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToHttpContentFormatter());
            _queryMessageFormatter = opts.QueryMessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new JsonStringToDictionary());

            _client = new HttpClient();
            if(opts.Headers is not null)
            {
                foreach (var header in opts.Headers)
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

            var url = _uri.ToString();
            if(_shouldUseQueryParameters)
            {
                var queryDict = await _queryMessageFormatter.FormatMessage(message);
                url = QueryHelpers.AddQueryString(url, queryDict);
            }

            var request = new HttpRequestMessage(_method, url);
            if(_shouldUseBody)
            {
                request.Content = await _bodyMessageFormatter.FormatMessage(message);
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

            if (_beforeSendMessage is not null)
            {
                await _beforeSendMessage(request);
            }

            _logger.LogTrace("Posting to {Uri}", _uri);

            var result = await _client.SendAsync(request, cancellationToken);
            _checkHttpResponse(result);
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
