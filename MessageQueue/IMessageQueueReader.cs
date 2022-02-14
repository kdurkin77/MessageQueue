using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageQueueReader<TMessage> : IDisposable
#if !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        MessageQueueReaderState State { get; }

        Task StartAsync(MessageQueueReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
