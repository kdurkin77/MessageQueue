namespace KM.MessageQueue
{
    public interface IMessageQueue<TMessage> : IReadOnlyQueue<TMessage>, IWriteOnlyQueue<TMessage> { }
}
