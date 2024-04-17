using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;
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
        internal ElasticsearchClientSettings ConnectionSettings { get; private set; } = new ElasticsearchClientSettings();
        internal string? Name { get; set; }

        /// <summary>
        /// The max number of messages that can be written at once
        /// </summary>
        public int? MaxWriteCount { get; set; }

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which converts the message to a <see cref="JObject"/>
        /// </summary>
        public IMessageFormatter<TMessage, JObject>? MessageFormatter { get; set; }

        private static ElasticsearchClientSettings CreateClientSettings(NodePool nodePool)
        {
            return
                new ElasticsearchClientSettings(nodePool, (serializer, settings) =>
                {
                    return new DefaultSourceSerializer(settings, options =>
                    {
                        options.Converters.Add(new JTokenConverter());
                    });
                });
        }

        /// <summary>
        /// Give a name to this queue
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ElasticSearchMessageQueueOptions<TMessage> UseName(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = name.Trim();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"{nameof(name)} is required", nameof(name));
            }

            Name = name;

            return this;
        }

        /// <summary>
        /// Sets up the <see cref="ConnectionSettings"/> using a <see cref="Uri"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="configureSettings"></param>
        /// <returns>ElasticSearchMessageQueueOptions</returns>
        public ElasticSearchMessageQueueOptions<TMessage> UseConnectionUri(Uri uri, Action<ElasticsearchClientSettings> configureSettings)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            ConnectionSettings = CreateClientSettings(new SingleNodePool(uri));
            configureSettings(ConnectionSettings);
            return this;
        }

        public ElasticSearchMessageQueueOptions<TMessage> UseApiKey(string cloudId, string apiKey, Action<ElasticsearchClientSettings> configureSettings)
        {
            if (string.IsNullOrWhiteSpace(cloudId))
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

            ConnectionSettings = CreateClientSettings(new CloudNodePool(cloudId, new ApiKey(apiKey)));
            configureSettings(ConnectionSettings);
            return this;
        }

        /// <summary>
        /// Sets up the <see cref="ConnectionSettings"/> using a collection of <see cref="Uri"/> to create a <see cref="SniffingConnectionPool"/>
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="configureSettings"></param>
        /// <returns>ElasticSearchMessageQueueOptions</returns>
        public ElasticSearchMessageQueueOptions<TMessage> UseConnectionPool(IEnumerable<Uri> uris, Action<ElasticsearchClientSettings> configureSettings)
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

            ConnectionSettings = CreateClientSettings(new StaticNodePool(uris));
            configureSettings(ConnectionSettings);
            return this;
        }
    }
}
