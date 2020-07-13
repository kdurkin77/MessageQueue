using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageHandler<TMessage>
    {
        Task<CompletionResult> HandleMessageAsync(TMessage message, MessageAttributes attributes, object? userData, CancellationToken cancellationToken);
        Task HandleErrorAsync(Exception error, CancellationToken cancellationToken);
    }
}
