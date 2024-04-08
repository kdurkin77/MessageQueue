using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface for a bulk queue that only reads/subscribes
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IBulkReadOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// The name that identifies this queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the bulk reader for the <see cref="IBulkMessageQueueReader{TMessage}"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IBulkMessageQueueReader<TMessage>> GetBulkReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken);
    }
}
