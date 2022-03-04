namespace KM.MessageQueue
{
    /// <summary>
    /// An interface for a formatter for an <see cref="IMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessageIn"></typeparam>
    /// <typeparam name="TMessageOut"></typeparam>
    public interface IMessageFormatter<TMessageIn, TMessageOut>
    {
        /// <summary>
        /// Converts a messages from <typeparamref name="TMessageOut"/> to <typeparamref name="TMessageIn"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        TMessageIn RevertMessage(TMessageOut message);

        /// <summary>
        /// Converts a message from <typeparamref name="TMessageIn"/> to <typeparamref name="TMessageOut"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        TMessageOut FormatMessage(TMessageIn message);
    }
}
