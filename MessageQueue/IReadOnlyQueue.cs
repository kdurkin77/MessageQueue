using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IReadOnlyQueue<TMessage> : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        Task<IMessageReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken);
    }
}
