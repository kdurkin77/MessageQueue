using KM.MessageQueue.Formatters.Specialized.Composition;

namespace KM.MessageQueue
{
    public static class Extensions
    {
        public static IMessageFormatter<TMessageIn, TMessageOut1> Compose<TMessageIn, TMessageOut0, TMessageOut1>(this IMessageFormatter<TMessageIn, TMessageOut0> lhs, IMessageFormatter<TMessageOut0, TMessageOut1> rhs)
        {
            return new CompositionFormatter<TMessageIn, TMessageOut0, TMessageOut1>(lhs, rhs);
        }
    }
}
