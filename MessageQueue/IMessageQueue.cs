namespace KM.MessageQueue
{
    public interface IMessageQueue<TMessage> : IReadOnlyMessageQueue<TMessage>, IWriteOnlyMessageQueue<TMessage> { }
}
