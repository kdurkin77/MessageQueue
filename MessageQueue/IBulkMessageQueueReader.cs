using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A disposable interface to bulk read messages from a <see cref="IBulkMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IBulkMessageQueueReader<TMessage> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// A name to identify this queue reader
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Read bulk messages from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CompletionResult> BulkReadMessageAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Read bulk messages from the queue
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="action"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(CompletionResult CompletionResult, TResult Result)> BulkReadMessageAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to read bulk messages from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(bool, CompletionResult)> TryBulkReadMessageAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to read bulk messages from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryBulkReadMessageAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, int count, CancellationToken cancellationToken);
    }
}
