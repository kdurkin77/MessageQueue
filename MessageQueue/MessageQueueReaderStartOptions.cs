using System;

namespace KM.MessageQueue
{
    public sealed class MessageQueueReaderStartOptions<TMessageIn, TMessageOut>
    {
        public MessageQueueReaderStartOptions(IMessageHandler<TMessageIn, TMessageOut> messageHandler)
        {
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public IMessageHandler<TMessageIn, TMessageOut> MessageHandler { get; }
        public int? PrefetchCount { get; set; }
        public string? SubscriptionName { get; set; }
        public object? UserData { get; set; }
    }
}
