namespace KM.MessageQueue
{
    /// <summary>
    /// Options required for an <see cref="IMessageQueueReader<TMessage>"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class MessageQueueReaderOptions<TMessage>
    {
        /// <summary>
        /// Optional name to identify this queue reader
        /// </summary>
        public string? Name { get; set; }

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
