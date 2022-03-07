using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
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
        internal ManagedMqttClientOptions ManagedMqttClientOptions { get; set; } = new ManagedMqttClientOptions();

        internal Func<byte[], MessageAttributes, MqttApplicationMessage> MessageBuilder { get; set; } = 
            (payload, attributes) => 
            new MqttApplicationMessageBuilder()
                .WithTopic(attributes.Label)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

        /// <summary>
        /// Configures the <see cref="MqttApplicationMessageBuilder"/> to be used to upload the message to the queue
        /// </summary>
        /// <param name="configureBuilder"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UseMessageBuilder(Action<MqttApplicationMessageBuilder> configureBuilder)
        {
            if (configureBuilder is null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }

            MessageBuilder = (payload, attributes) =>
            {
                var baseBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic(attributes.Label)
                    .WithPayload(payload);

                configureBuilder(baseBuilder);
                return baseBuilder.Build();
            };
        }

        /// <summary>
        /// Congfigures the <see cref="MQTTnet.Extensions.ManagedClient.ManagedMqttClientOptions"/> using a <see cref="ManagedMqttClientOptionsBuilder"/>
        /// </summary>
        /// <param name="configureSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UseManagedMqttClientOptionsBuilder(Action<ManagedMqttClientOptionsBuilder> configureSettings)
        {
            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            var builder = new ManagedMqttClientOptionsBuilder();
            configureSettings(builder);
            ManagedMqttClientOptions = builder.Build();
        }

        /// <summary>
        /// Congfigures the <see cref="MQTTnet.Extensions.ManagedClient.ManagedMqttClientOptions"/>
        /// </summary>
        /// <param name="configureSettings"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void UseManagedMqttClientOptions(Action<ManagedMqttClientOptions> configureSettings)
        {
            if (configureSettings is null)
            {
                throw new ArgumentNullException(nameof(configureSettings));
            }

            configureSettings(ManagedMqttClientOptions);
        }

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which serializes the message to JSON and then converts it to bytes
        /// </summary>
        public IMessageFormatter<TMessage, byte[]> MessageFormatter { get; set; } = new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
    }
}
