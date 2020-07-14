using Microsoft.Azure.ServiceBus;

namespace KM.MessageQueue.Azure.Topic
{
    public sealed class AzureTopicOptions<TMessage>
    {
        public string? Endpoint { get; set; }
        public string? EntityPath { get; set; }
        public string? SharedAccessKeyName { get; set; }
        public string? SharedAccessKey { get; set; }
        public string? SubscriptionName { get; set; }
        public TransportType TransportType { get; set; }
    }
}
