using MQTTnet.Formatter;
using System;

namespace KM.MessageQueue.Mqtt.Tcp
{
    public sealed class TcpMqttMessageQueueOptions
    {
        public string? Url { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool? WithCleanSession { get; set; }
        public int? MaxPendingMessages { get; set; }
        public TimeSpan? AutoReconnectDelay { get; set; }
        public TimeSpan? CommunicationTimeout { get; set; }
        public MqttProtocolVersion? ProtocolVersion { get; set; }
    }
}
