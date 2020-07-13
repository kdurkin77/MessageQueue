using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessageReader<TMessage> : IMessageReader<TMessage>
    {
        private enum ReaderState
        {
            Stopped = 0,
            StopRequested = 1,
            Running = 2
        }

        private bool _disposed = false;
        private readonly DiskMessageQueue<TMessage> _queue;

        private readonly SemaphoreSlim _sync = new SemaphoreSlim(1, 1);

        private ReaderState _readerState = ReaderState.Stopped;
        private Task? _readerTask = null;
        private CancellationTokenSource? _readerTokenSource = null;

        public DiskMessageReader(DiskMessageQueue<TMessage> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public Task StartAsync(IMessageHandler<TMessage> messageHandler, CancellationToken cancellationToken)
        {
            return StartAsync(messageHandler, null, cancellationToken);
        }

        public async Task StartAsync(IMessageHandler<TMessage> messageHandler, object? userData, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_readerState == ReaderState.Running)
                {
                    throw new NotSupportedException($"{nameof(DiskMessageReader<TMessage>)} is already started");
                }

                if (_readerState == ReaderState.StopRequested)
                {
                    throw new NotSupportedException($"{nameof(DiskMessageReader<TMessage>)} is stopping");
                }

                _readerTokenSource = new CancellationTokenSource();

                _readerTask = Task.Run(() => ReaderLoop(messageHandler, userData, cancellationToken));

                _readerState = ReaderState.Running;
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
                await messageHandler.HandleErrorAsync(ex, cancellationToken).ConfigureAwait(false);
                throw;
            }
            finally
            {
                _readerTokenSource?.Dispose();
                _readerTokenSource = null;
                _readerTask = null;
                _readerState = ReaderState.Stopped;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_readerState == ReaderState.Stopped)
                {
                    throw new NotSupportedException($"{nameof(DiskMessageReader<TMessage>)} is already stopped");
                }

                if (_readerState == ReaderState.StopRequested)
                {
                    throw new NotSupportedException($"{nameof(DiskMessageReader<TMessage>)} is already stopping");
                }

                if (_readerTokenSource is null)
                {
                    throw new SystemException($"{nameof(DiskMessageReader<TMessage>)}.{nameof(_readerTokenSource)} is null");
                }

                _readerTokenSource.Cancel();
                _readerState = ReaderState.StopRequested;
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
                if (_readerTokenSource != null && !_readerTokenSource.IsCancellationRequested)
                {
                    _readerTokenSource.Cancel();
                    _readerTask?.ConfigureAwait(false).GetAwaiter().GetResult();
                }
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

            if (_readerTokenSource != null && !_readerTokenSource.IsCancellationRequested)
            {
                _readerTokenSource.Cancel();
                var task = _readerTask;
                if (task != null)
                {
                    await task.ConfigureAwait(false);
                }
            }

            Dispose(false);
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }
#endif
    }
}
