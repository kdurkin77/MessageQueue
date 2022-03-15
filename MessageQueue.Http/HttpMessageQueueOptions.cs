using KM.MessageQueue.Formatters.ObjectToJsonObject;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace KM.MessageQueue.Http
{
    /// <summary>
    /// Options for <see cref="HttpMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class HttpMessageQueueOptions<TMessage>
    {
        internal bool ShouldUseBody { get; set; } = true;
        internal bool ShouldUseQueryParameters { get; set; } = false;

        /// <summary>
        /// The URL to do the HTTP request
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// The type of HTTP request - Get, Post, Patch, etc
        /// </summary>
        public HttpMethod Method { get; set; } = HttpMethod.Get;

        /// <summary>
        /// Any headers required
        /// </summary>
        public IDictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// The formatter to use to format the message for the body of the HTTP request
        /// The default is to convert the message to a Json string and then to HttpContext
        /// </summary>
        public IMessageFormatter<TMessage, HttpContent> BodyMessageFormatter { get; set; } = new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToHttpContentFormatter());

        /// <summary>
        /// The formatter to use to format the message for the query parameters of the HTTP request
        /// The default is to convert the message to a Json string and then convert to a Dictionary
        /// </summary>
        public IMessageFormatter<TMessage, IDictionary<string, string>> QueryMessageFormatter { get; set; } = new ObjectToJsonStringFormatter<TMessage>().Compose(new JsonStringToDictionary());
        
        /// <summary>
        /// Function to check to see if we get a successful response from the HTTP request. By default, this function
        /// simply checks that there is successful status code response
        /// </summary>
        public Action<HttpResponseMessage?> CheckHttpResponse { get; set; } =
            message =>
            {
                if(message is null) 
                {
                    throw new HttpRequestException("No response returned");
                }
                message.EnsureSuccessStatusCode();
            };

        /// <summary>
        /// Use the default formatter to add the message to the body of the request
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
