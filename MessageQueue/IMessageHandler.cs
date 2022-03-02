using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageHandler<TMessageIn, TMessageOut>
    {
        Task<CompletionResult> HandleMessageAsync(IMessageFormatter<TMessageIn, TMessageOut> formatter, TMessageOut messageBytes, MessageAttributes attributes, object? userData, CancellationToken cancellationToken);
        Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken);
    }
}
