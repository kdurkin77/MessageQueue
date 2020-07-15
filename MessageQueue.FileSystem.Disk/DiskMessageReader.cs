using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessageReader<TMessage> : IMessageReader<TMessage>
    {
        private bool _disposed = false;
        private readonly DiskMessageQueue<TMessage> _queue;

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        public MessageReaderState State { get; private set; } = MessageReaderState.Stopped;
        private Task? _readerTask = null;
        private CancellationTokenSource? _readerTokenSource = null;

        public DiskMessageReader(DiskMessageQueue<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public async Task StartAsync(MessageReaderStartOptions<TMessage> startOptions, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (startOptions is null)
            {
                throw new ArgumentNullException(nameof(startOptions));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageReaderState.Running)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageReader<TMessage>)} is already started");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageReader<TMessage>)} is stopping");
                }

                _readerTokenSource = new CancellationTokenSource();

                _readerTask = Task.Run(() => ReaderLoop(startOptions.MessageHandler, startOptions.UserData, cancellationToken));

                State = MessageReaderState.Running;
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
                        throw new SystemException($"{nameof(DiskMessageReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
                    }

                    if (source.IsCancellationRequested)
                    {
                        break;
                    }

                    var gotMessage = await _queue.TryReadMessageAsync(messageHandler.HandleMessageAsync, userData, source.Token).ConfigureAwait(false);
                    if (!gotMessage)
                    {
                        await Task.Delay(1);
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
                State = MessageReaderState.Stopped;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (State == MessageReaderState.Stopped)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageReader<TMessage>)} is already stopped");
                }

                if (State == MessageReaderState.StopRequested)
                {
                    throw new InvalidOperationException($"{nameof(DiskMessageReader<TMessage>)} is already stopping");
                }

                if (_readerTokenSource is null)
                {
                    throw new SystemException($"{nameof(DiskMessageReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
                }

                _readerTokenSource.Cancel();
                State = MessageReaderState.StopRequested;
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
                throw new ObjectDisposedException(nameof(DiskMessageReader<TMessage>));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _readerTokenSource?.Cancel();
            }

            _disposed = true;
        }

        ~DiskMessageReader() => Dispose(false);

#if !NETSTANDARD2_0

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _readerTokenSource?.Cancel();

            Dispose(false);
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif

    }
}
