using Azure.Messaging.ServiceBus;
using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using System;
using System.Linq;

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
        public IMessageFormatter<TMessage, byte[]> MessageFormatter { get; set; } = new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> using a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public void UseConnectionString(string connectionString)
        {
            UseConnectionString(connectionString, options => { });
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> using a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="configureSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UseConnectionString(string connectionString, Action<ServiceBusClientOptions> configureSettings)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if(configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }
            var keyValuePairs = connectionString.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            EntityPath = keyValuePairs.Where(key => string.Equals(key, "EntityPath", StringComparison.OrdinalIgnoreCase)).First();
            ConnectionString = connectionString;
            configureSettings(ServiceBusClientOptions);
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        public void UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey)
        {
            UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, null, (_) => { });
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <param name="configureSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, Action<ServiceBusClientOptions> configureSettings)
        {
            UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, null, configureSettings);
        }

        /// <summary>
        /// Setup the <see cref="ServiceBusClient"/> by building a connection string
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="entityPath"></param>
        /// <param name="sharedAccessKeyName"></param>
        /// <param name="sharedAccessKey"></param>
        /// <param name="transportType"></param>
        public void UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, string transportType)
        {
            UseConnectionStringBuilder(endpoint, entityPath, sharedAccessKeyName, sharedAccessKey, transportType, (_) => { });
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
        /// <exception cref="ArgumentNullException"></exception>
        public void UseConnectionStringBuilder(string endpoint, string entityPath, string sharedAccessKeyName, string sharedAccessKey, string? transportType, Action<ServiceBusClientOptions> configureSettings)
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

            if(string.IsNullOrWhiteSpace(sharedAccessKey))
            {
                throw new ArgumentNullException(nameof(sharedAccessKey));
            }

            if (string.IsNullOrWhiteSpace(transportType))
            {
                throw new ArgumentNullException(nameof(transportType));
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
        }
    }
}
