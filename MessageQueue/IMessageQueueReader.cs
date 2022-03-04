using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface to read messages from a <see cref="IMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageQueueReader<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// The state of the reader
        /// </summary>
        MessageQueueReaderState State { get; }

        /// <summary>
        /// The start function for the reader
        /// </summary>
        /// <param name="startOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartAsync(MessageQueueReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken);

        /// <summary>
        /// The stop function for the reader
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
