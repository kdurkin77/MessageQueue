namespace KM.MessageQueue
{
    public interface IMessageFormatter<TMessage>
    {
        TMessage BytesToMessage(byte[] bytes);
        byte[] MessageToBytes(TMessage message);
    }
}
