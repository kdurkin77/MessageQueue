using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageReader<TMessage> : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        MessageReaderState State { get; }

        Task StartAsync(IMessageHandler<TMessage> messageHandler, CancellationToken cancellationToken);
        Task StartAsync(IMessageHandler<TMessage> messageHandler, object? userData, CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
