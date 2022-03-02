namespace KM.MessageQueue
{
    public interface IMessageFormatter<TMessageIn, TMessageOut>
    {
        TMessageIn RevertMessage(TMessageOut bytes);
        TMessageOut FormatMessage(TMessageIn message);
    }
}
