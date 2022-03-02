using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue
{
    public interface IMessageQueueReader<TMessageIn, TMessageOut> : IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET
        , IAsyncDisposable
#endif
    {
        MessageQueueReaderState State { get; }

        Task StartAsync(MessageQueueReaderStartOptions<TMessageIn, TMessageOut> startOptions, CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
