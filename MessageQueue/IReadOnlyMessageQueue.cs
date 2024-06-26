﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface for a queue that only reads/subscribes
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IReadOnlyMessageQueue<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// The name that identifies this queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Max number of messages that can be read at once
        /// </summary>
        int MaxReadCount { get; }

        /// <summary>
        /// Gets the reader for the <see cref="IReadOnlyMessageQueue{TMessage}"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken);
    }
}
