using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace KM.MessageQueue.Http
{
    /// <summary>
    /// Options for <see cref="HttpMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class HttpMessageQueueOptions<TMessage>
    {
        internal bool? ShouldUseBody { get; set; }
        internal bool? ShouldUseQueryParameters { get; set; }
        internal IMessageFormatter<TMessage, HttpContent>? BodyMessageFormatter { get; set; }
        internal IMessageFormatter<TMessage, IDictionary<string, string>>? QueryMessageFormatter { get; set; }

        /// <summary>
        /// Optional name to identify this queue
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The URL to do the HTTP request
        /// </summary>
        public Uri? Uri { get; set; }

        /// <summary>
        /// The type of HTTP request, default is Get
        /// </summary>
        public HttpMethod? Method { get; set; }

        /// <summary>
        /// Any headers required
        /// </summary>
        public IDictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Function to check to see if we get a successful response from the HTTP request. By default, this function
        /// simply checks that there is successful status code response
        /// </summary>
        public Action<HttpResponseMessage?>? CheckHttpResponse { get; set; }

        /// <summary>
        /// A callback function to be run before the HTTP message is sent
        /// </summary>
        public Func<HttpRequestMessage, Task>? BeforeSendMessage { get; set; }

        /// <summary>
        /// Use the default formatter to add the message to the body of the request
        /// The default forrmatter converts the message to a Json string and then to HttpContext
        /// </summary>
        /// <returns></returns>
        public HttpMessageQueueOptions<TMessage> UseBody()
        {
            ShouldUseBody = true;
            return this;
        }

        /// <summary>
        /// Use the formatter specified to add the message to the body of the request
        /// </summary>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public HttpMessageQueueOptions<TMessage> UseBody(IMessageFormatter<TMessage, HttpContent> formatter)
        {
            BodyMessageFormatter = formatter;
            return UseBody();
        }

        /// <summary>
        /// Use the default formatter to add the message to the query parameters of the request
        /// The default formatter converts the message to a Json string and then to a Dictionary
        /// </summary>
        /// <returns></returns>
        public HttpMessageQueueOptions<TMessage> UseQueryParameters()
        {
            ShouldUseQueryParameters = true;
            return this;
        }

        /// <summary>
        /// Use the formatter specified to add the message to the query parameters of the request
        /// </summary>
        /// <param name="formatter"></param>
        /// <returns></returns>
        public HttpMessageQueueOptions<TMessage> UseQueryParameters(IMessageFormatter<TMessage, IDictionary<string, string>> formatter)
        {
            QueryMessageFormatter = formatter;
            return UseQueryParameters();
        }
    }
}
