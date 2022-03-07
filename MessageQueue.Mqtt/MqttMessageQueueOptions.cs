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
        /// <summary>
        /// Options to create the <see cref="ManagedMqttClient"/>, use <see cref="ManagedMqttClientOptionsBuilder"/>
        /// </summary>
        internal ManagedMqttClientOptions ManagedMqttClientOptions { get; set; } = new ManagedMqttClientOptions();

        internal Func<byte[], MessageAttributes, MqttApplicationMessage> MessageBuilder { get; set; } = 
            (payload, attributes) => 
            new MqttApplicationMessageBuilder()
                .WithTopic(attributes.Label)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

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
