using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A handler to take care of message received by the <see cref="IMessageQueueReader{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IMessageHandler<TMessage>
    {
        /// <summary>
        /// A function to handle the messages
        /// </summary>
        /// <param name="messageBytes"></param>
        /// <param name="attributes"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<CompletionResult> HandleMessageAsync(TMessage messageBytes, MessageAttributes attributes, object? userData, CancellationToken cancellationToken);
        
        /// <summary>
        /// A function to handle the errors
        /// </summary>
        /// <param name="error"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken);
    }
}
