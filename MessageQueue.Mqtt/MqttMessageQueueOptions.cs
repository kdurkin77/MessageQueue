using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;

namespace KM.MessageQueue.Mqtt
{
    /// <summary>
    /// Options for <see cref="MqttMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class MqttMessageQueueOptions<TMessage>
    {
        internal ManagedMqttClientOptionsBuilder? ClientOptionsBuilder { get; set; }

        internal Func<byte[], MessageAttributes, MqttApplicationMessage>? MessageBuilder { get; set; }

        /// <summary>
        /// Optional name to identify this queue
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which serializes the message to JSON and then converts it to bytes
        /// </summary>
        public IMessageFormatter<TMessage, byte[]>? MessageFormatter { get; set; }

        /// <summary>
        /// Configures the <see cref="MqttApplicationMessageBuilder"/> to be used to upload the message to the queue
        /// </summary>
        /// <param name="configureBuilder"></param>
        /// <returns>MqttMessageQueueOptions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MqttMessageQueueOptions<TMessage> UseMessageBuilder(Action<MqttApplicationMessageBuilder> configureBuilder)
        {
            if (configureBuilder is null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }

            MessageBuilder =
                (payload, attributes) =>
                {
                    var messageBuilder = new MqttApplicationMessageBuilder()
                        .WithTopic(attributes.Label)
                        .WithPayload(payload);

                    configureBuilder(messageBuilder);
                    return messageBuilder.Build();
                };

            return this;
        }

        /// <summary>
        /// Congfigures the <see cref="MQTTnet.Client.ManagedMqttClientOptionsBuilder"/> using a <see cref="ManagedMqttClientOptionsBuilder"/>
        /// </summary>
        /// <param name="configureSettings"></param>
        /// <returns>MqttMessageQueueOptions</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MqttMessageQueueOptions<TMessage> UseClientOptionsBuilder(Action<ManagedMqttClientOptionsBuilder> configureSettings)
        {
            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            var builder = new ManagedMqttClientOptionsBuilder();
            configureSettings(builder);

            ClientOptionsBuilder = builder;

            return this;
        }

        ///// <summary>
        ///// Congfigures the <see cref="MQTTnet.Extensions.ManagedClient.ManagedMqttClientOptions"/>
        ///// </summary>
        ///// <param name="configureSettings"></param>
        ///// <returns>MqttMessageQueueOptions</returns>
        ///// <exception cref="ArgumentNullException"></exception>
        //public MqttMessageQueueOptions<TMessage> UseManagedMqttClientOptions(Action<ManagedMqttClientOptions> configureSettings)
        //{
        //    if (configureSettings is null)
        //    {
        //        throw new ArgumentNullException(nameof(configureSettings));
        //    }

        //    configureSettings(ManagedMqttClientOptions);
        //    return this;
        //}
    }
}
