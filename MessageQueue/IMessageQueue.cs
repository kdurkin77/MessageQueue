namespace KM.MessageQueue
{
    public interface IMessageQueue<TMessageIn, TMessageOut> : IReadOnlyMessageQueue<TMessageIn, TMessageOut>, IWriteOnlyMessageQueue<TMessageIn> { }
}
