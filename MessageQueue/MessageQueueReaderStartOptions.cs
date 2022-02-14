using System;

namespace KM.MessageQueue
{
    public sealed class MessageQueueReaderStartOptions<TMessage>
    {
        public MessageQueueReaderStartOptions(IMessageHandler<TMessage> messageHandler)
        {
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public IMessageHandler<TMessage> MessageHandler { get; }
        public int? PrefetchCount { get; set; }
        public string? SubscriptionName { get; set; }
        public object? UserData { get; set; }
    }
}
