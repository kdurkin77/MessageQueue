namespace KM.MessageQueue
{
    /// <summary>
    /// Indicates the disposition of processing a message
    /// </summary>
    public enum CompletionResult
    {
        /// <summary>
        /// The messages has been processed and is complete
        /// </summary>
        Complete = 0,

        /// <summary>
        /// The processor has given up on this message and it should be retried or consumed by another process
        /// </summary>
        Abandon = 1
    }
}
