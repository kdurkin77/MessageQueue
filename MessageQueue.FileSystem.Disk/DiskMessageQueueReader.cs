using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        private bool _disposed = false;
        private readonly DiskMessageQueue<TMessage> _queue;

        private readonly SemaphoreSlim _sync = new(1, 1);

        public MessageQueueReaderState State { get; private set; } = MessageQueueReaderState.Stopped;
        private Task? _readerTask = null;
        private CancellationTokenSource? _readerTokenSource = null;

        public DiskMessageQueueReader(DiskMessageQueue<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public async Task StartAsync(MessageQueueReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (startOptions is null)
            {
                throw new ArgumentNullException(nameof(startOptions));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageQueueReaderState.Running)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageQueueReader<TMessage>)} is already started");
                }

                if (State == MessageQueueReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageQueueReader<TMessage>)} is stopping");
                }

                _readerTokenSource = new CancellationTokenSource();

                _readerTask = Task.Run(() => ReaderLoop(startOptions.MessageHandler, startOptions.UserData, cancellationToken), _readerTokenSource.Token);

                State = MessageQueueReaderState.Running;
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task ReaderLoop(IMessageHandler<TMessage> messageHandler, object? userData, CancellationToken cancellationToken)
        {
            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            try
            {
                while (true)
                {
                    var source = _readerTokenSource;
                    if (source is null)
                    {
                        throw new SystemException($"{nameof(DiskMessageQueueReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
                    }

                    if (source.IsCancellationRequested)
                    {
                        break;
                    }

                    var gotMessage = await _queue.TryReadMessageAsync(messageHandler.HandleMessageAsync, userData, source.Token).ConfigureAwait(false);
                    if (!gotMessage)
                    {
                        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await messageHandler.HandleErrorAsync(ex, userData, cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                _readerTokenSource?.Dispose();
                _readerTokenSource = null;
                _readerTask = null;
                State = MessageQueueReaderState.Stopped;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageQueueReaderState.Stopped)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageQueueReader<TMessage>)} is already stopped");
                }

                if (State == MessageQueueReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageQueueReader<TMessage>)} is already stopping");
                }

                if (_readerTokenSource is null)
                {
                    throw new SystemException($"{nameof(DiskMessageQueueReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
                }

                _readerTokenSource.Cancel();
                State = MessageQueueReaderState.StopRequested;
            }
            finally
            {
                _sync.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DiskMessageQueueReader<TMessage>));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _readerTokenSource?.Cancel();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _readerTokenSource?.Cancel();

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
