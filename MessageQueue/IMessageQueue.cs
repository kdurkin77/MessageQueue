using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageQueue
{
    public interface IMessageQueue<TMessage> : IDisposable
    {
        Task PostMessageAsync(TMessage message, CancellationToken ct);
        Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken);
    }
}
