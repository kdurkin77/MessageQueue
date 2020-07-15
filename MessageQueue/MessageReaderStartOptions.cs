using System;

namespace KM.MessageQueue
{
    public sealed class MessageReaderStartOptions<TMessage>
    {
        public MessageReaderStartOptions(IMessageHandler<TMessage> messageHandler)
        {
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        public IMessageHandler<TMessage> MessageHandler { get; }
        public string? SubscriptionName { get; set; }
        public object? UserData { get; set; }
    }
}
