using KM.MessageQueue.Formatters.JsonStringToDictionary;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToHttpContent;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Http
{
    public sealed class HttpMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public HttpMessageQueue(ILogger<HttpMessageQueue<TMessage>> logger, IOptions<HttpMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _uri = opts.Uri ?? throw new ArgumentException($"{nameof(opts.Uri)} is required", nameof(options));

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
                    return Task.CompletedTask;
                });
            _beforeSendMessage = opts.BeforeSendMessage;
            _bodyMessageFormatter = opts.BodyMessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToHttpContentFormatter());
            _queryMessageFormatter = opts.QueryMessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new JsonStringToDictionary());

            _client = new HttpClient();
            if (opts.Headers is { } headers)
            {
                foreach (var header in headers)
                {
                    _client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            Name = opts.Name ?? nameof(HttpMessageQueue<TMessage>);

            _logger.LogTrace($"{Name} initialized");
        }


        private bool _disposed = false;

        private readonly ILogger _logger;
        internal readonly HttpClient _client;
        internal readonly Uri _uri;
        internal readonly HttpMethod _method;
        internal readonly bool _shouldUseBody;
        internal readonly bool _shouldUseQueryParameters;
        internal readonly Func<HttpResponseMessage?, Task> _checkHttpResponse;
        internal readonly Func<HttpRequestMessage, Task>? _beforeSendMessage;
        internal readonly IMessageFormatter<TMessage, HttpContent> _bodyMessageFormatter;
        internal readonly IMessageFormatter<TMessage, IDictionary<string, string>> _queryMessageFormatter;

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

            var url = _uri.ToString();
            var (message, attributes) = messages.Single();
            if (_shouldUseQueryParameters)
            {
                var queryDict = await _queryMessageFormatter.FormatMessage(message).ConfigureAwait(false);
                url = QueryHelpers.AddQueryString(url, (IDictionary<string, string?>)queryDict);
            }

            var request = new HttpRequestMessage(_method, url);
            if (_shouldUseBody)
            {
                request.Content = await _bodyMessageFormatter.FormatMessage(message).ConfigureAwait(false);
                if (attributes.ContentType is { } contentType)
                {
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                }
            }

            if (attributes.UserProperties is { } userProperties)
            {
                foreach (var property in userProperties)
                {
#if NET
                    _ = request.Options.TryAdd(property.Key, property.Value);
#else
                    request.Properties.Add(property);
#endif
                }
            }

            if (attributes.Label is { } label)
            {
#if NET
                _ = request.Options.TryAdd("Label", label);
#else
                request.Properties.Add("Label", label);
#endif
            }

            if (_beforeSendMessage is { } beforeSendMessage)
            {
                await beforeSendMessage(request).ConfigureAwait(false);
            }

            _logger.LogTrace($"{Name} {nameof(PostManyMessagesAsync)} posting to {{Uri}}", _uri);

            var result = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await _checkHttpResponse(result);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            throw new NotSupportedException();
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

            await Task.CompletedTask;
        }

#endif
    }
}
