using System;

namespace KM.MessageQueue
{
    /// <summary>
    /// Options required for an <see cref="IMessageQueueReader<TMessage>"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class MessageQueueReaderStartOptions<TMessage>
    {
        public MessageQueueReaderStartOptions(IMessageHandler<TMessage> messageHandler)
        {
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
        }

        /// <summary>
        /// An <see cref="IMessageHandler{TMessage}"/> to handle received messages
        /// </summary>
        public IMessageHandler<TMessage> MessageHandler { get; }

        /// <summary>
        /// Optional prefetch count
        /// </summary>
        public int? PrefetchCount { get; set; }

        /// <summary>
        /// Optional subscription name
        /// </summary>
        public string? SubscriptionName { get; set; }

        /// <summary>
        /// Optional user data
        /// </summary>
        public object? UserData { get; set; }
    }
}
