using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace KM.MessageQueue.Azure.Topic
{
    /// <summary>
    /// Options for <see cref="AzureTopicMessageQueue{TMessage}"/>
    /// </summary>
    public sealed class AzureTopicMessageQueueOptions<TMessage>
    {
        internal string? ConnectionString { get; set; }
        internal ServiceBusClientOptions ServiceBusClientOptions { get; set; } = new ServiceBusClientOptions();
        internal string? EntityPath { get; set; }

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which serializes the message to JSON and then converts it to bytes
        /// </summary>
        public IMessageFormatter<TMessage, byte[]>? MessageFormatter { get; set; }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> using a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionString(string connectionString)
        {
            return UseConnectionString(connectionString, options => { });
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> using a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="configureSettings"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionString(string connectionString, Action<ServiceBusClientOptions> configureSettings)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            var keyValuePairs = Regex.Matches(connectionString, @"\s*(?<key>[^;=]+)\s*=\s*((?<value>[^'][^;]*)|'(?<value>[^']*)')")
                .Cast<Match>()
                .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value);

            var entityPath = keyValuePairs.SingleOrDefault(k => string.Equals(k.Key, "EntityPath", StringComparison.OrdinalIgnoreCase)).Value;
            if (entityPath is null)
            {
                //check the end of the endpoint for entity path
                var endpoint = keyValuePairs.SingleOrDefault(k => string.Equals(k.Key, "Endpoint", StringComparison.OrdinalIgnoreCase)).Value;
                if (endpoint is null)
                {
                    throw new ArgumentException($"{nameof(connectionString)} must include Endpoint", nameof(connectionString));
                }

                var index = endpoint.LastIndexOf("/");
                if (index == -1)
                {
                    throw new ArgumentException($"{nameof(connectionString)} must include EntityPath or have it specified in Endpoint");
                }

                entityPath = endpoint.Substring(index + 1);
            }

            if (string.IsNullOrWhiteSpace(entityPath))
            {
                throw new ArgumentException($"{nameof(connectionString)} must include EntityPath or have it specified in Endpoint");
            }

            EntityPath = entityPath;
            ConnectionString = connectionString;
            configureSettings(ServiceBusClientOptions);
            return this;
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey)
        {
            return UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, null, (_) => { });
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <param name="configureSettings"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, Action<ServiceBusClientOptions> configureSettings)
        {
            return UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, null, configureSettings);
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <param name="transportType"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, string transportType)
        {
            return UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, transportType, (_) => { });
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <param name="transportType"></param>
        /// <param name="configureSettings"></param>
        /// <returns>AzureTopicMessageQueueOptions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public AzureTopicMessageQueueOptions<TMessage> UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, string? transportType, Action<ServiceBusClientOptions> configureSettings)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (string.IsNullOrWhiteSpace(entityPath))
            {
                throw new ArgumentNullException(nameof(entityPath));
            }

            if (string.IsNullOrWhiteSpace(sharedAccessKeyName))
            {
                throw new ArgumentNullException(nameof(sharedAccessKeyName));
            }

            if (string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                throw new ArgumentNullException(nameof(sharedAccessKey));
            }

            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            EntityPath = entityPath;
            ConnectionString = $"Endpoint={endpoint};SharedAccessKeyName={sharedAccessKeyName};SharedAccessKey={sharedAccessKey};EntityPath={entityPath};";
            if (transportType is not null)
            {
                ConnectionString += "TransportType={transportType};";
            }

            configureSettings(ServiceBusClientOptions);
            return this;
        }
    }
}
