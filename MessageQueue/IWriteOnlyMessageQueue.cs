using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface for a queue that only writes/posts
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IWriteOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// A name to identify this queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Post a message to the queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PostMessageAsync(TMessage message, CancellationToken cancellationToken);

        /// <summary>
        /// Posts a message with attributes to the queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="attributes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken);
    }
}
