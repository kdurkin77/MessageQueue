namespace KM.MessageQueue
{
    public interface IMessageFormatter<TMessage>
    {
        byte[] Format(TMessage message);
    }
}
