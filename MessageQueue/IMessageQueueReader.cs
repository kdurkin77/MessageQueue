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
        /// A name to identify this queue reader
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Read a message from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CompletionResult> ReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken);

        /// <summary>
        /// Read a message from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(CompletionResult CompletionResult, TResult Result)> ReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to read a message from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(bool, CompletionResult)> TryReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken);

        /// <summary>
        /// Attempt to read a message from the queue
        /// </summary>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken);
    }
}
