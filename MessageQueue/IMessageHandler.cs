using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageHandler<TMessage>
    {
        Task<CompletionResult> HandleMessageAsync(IMessageFormatter<TMessage> formatter, byte[] messageBytes, MessageAttributes attributes, object? userData, CancellationToken cancellationToken);
        Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken);
    }
}
