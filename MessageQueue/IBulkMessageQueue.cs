namespace KM.MessageQueue
{
    /// <summary>
    /// An interface for a queue implementation that inherits <see cref="IBulkReadOnlyMessageQueue{TMessage}"/> and  <see cref="IBulkWriteOnlyMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IBulkMessageQueue<TMessage> : IBulkReadOnlyMessageQueue<TMessage>, IBulkWriteOnlyMessageQueue<TMessage> { }
}
