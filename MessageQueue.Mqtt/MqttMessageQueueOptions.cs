using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using MQTTnet.Extensions.ManagedClient;

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
        public ManagedMqttClientOptions ManagedMqttClientOptions { get; set; } = new ManagedMqttClientOptions();

        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which serializes the message to JSON and then converts it to bytes
        /// </summary>
        public IMessageFormatter<TMessage, byte[]> MessageFormatter { get; set; } = new JsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
    }
}
