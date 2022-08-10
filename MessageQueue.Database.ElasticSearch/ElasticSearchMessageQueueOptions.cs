using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KM.MessageQueue.Database.ElasticSearch
{
    /// <summary>
    /// Options for the <see cref="ElasticSearchMessageQueue{TMessage}"/>
    /// </summary>
    public sealed class ElasticSearchMessageQueueOptions<TMessage>
    {
        internal ConnectionSettings ConnectionSettings { get; private set; } = new ConnectionSettings();

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which converts the message to a <see cref="JObject"/>
        /// </summary>
        public IMessageFormatter<TMessage, JObject>? MessageFormatter { get; set; }

        /// <summary>
        /// Sets up the <see cref="ConnectionSettings"/> using a <see cref="Uri"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="configureSettings"></param>
        /// <returns>ElasticSearchMessageQueueOptions</returns>
        public ElasticSearchMessageQueueOptions<TMessage> UseConnectionUri(Uri uri, Action<ConnectionSettings> configureSettings)
        {
            if(uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if(configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            ConnectionSettings = new ConnectionSettings(uri);
            configureSettings(ConnectionSettings);
            return this;
        }

        public ElasticSearchMessageQueueOptions<TMessage> UseApiKey(string cloudId, string apiKey, Action<ConnectionSettings> configureSettings)
        {
            if(string.IsNullOrWhiteSpace(cloudId))
            {
                throw new ArgumentNullException(nameof(cloudId));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            ConnectionSettings = new ConnectionSettings(cloudId, new ApiKeyAuthenticationCredentials(apiKey));
            configureSettings(ConnectionSettings);
            return this;
        }

        /// <summary>
        /// Sets up the <see cref="ConnectionSettings"/> using a collection of <see cref="Uri"/> to create a <see cref="SniffingConnectionPool"/>
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="configureSettings"></param>
        /// <returns>ElasticSearchMessageQueueOptions</returns>
        public ElasticSearchMessageQueueOptions<TMessage> UseConnectionPool(IEnumerable<Uri> uris, Action<ConnectionSettings> configureSettings)
        {
            if (uris is null)
            {
                throw new ArgumentNullException(nameof(uris));
            }

            if (!uris.Any())
            {
                throw new ArgumentException($"{nameof(uris)} cannot be empty");
            }

            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            var connPool = new SniffingConnectionPool(uris);
            ConnectionSettings = new ConnectionSettings(connPool);
            configureSettings(ConnectionSettings);
            return this;
        }
    }
}
