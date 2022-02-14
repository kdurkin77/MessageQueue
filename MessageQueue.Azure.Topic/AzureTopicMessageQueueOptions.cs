using Azure.Messaging.ServiceBus;

namespace KM.MessageQueue.Azure.Topic
{
    public sealed class AzureTopicMessageQueueOptions
    {
        public string? Endpoint { get; set; }
        public string? EntityPath { get; set; }
        public string? SharedAccessKeyName { get; set; }
        public string? SharedAccessKey { get; set; }
        public ServiceBusTransportType? TransportType { get; set; }
    }
}
