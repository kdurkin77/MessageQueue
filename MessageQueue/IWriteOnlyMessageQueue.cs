using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IWriteOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        Task PostMessageAsync(TMessage message, CancellationToken cancellationToken);
        Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken);
    }
}
