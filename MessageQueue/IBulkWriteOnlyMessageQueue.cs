using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface for a bulk queue that only writes/posts
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IBulkWriteOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// A name to identify this queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Post bulk messages to the queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task BulkPostMessageAsync(IEnumerable<TMessage> message, CancellationToken cancellationToken);

        /// <summary>
        /// Post bulk messages to the queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="attributes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task BulkPostMessageAsync(IEnumerable<TMessage> message, MessageAttributes attributes, CancellationToken cancellationToken);
    }
}
