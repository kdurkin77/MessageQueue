using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IReadOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken);
    }
}
