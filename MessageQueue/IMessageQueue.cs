namespace KM.MessageQueue
{
    /// <summary>
    /// An interface for a queue implementation that inherits <see cref="IReadOnlyMessageQueue{TMessage}"/> and  <see cref="IWriteOnlyMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageQueue<TMessage> : IReadOnlyMessageQueue<TMessage>, IWriteOnlyMessageQueue<TMessage> { }
}
