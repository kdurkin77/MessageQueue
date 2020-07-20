using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class Forwarder<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly ForwarderOptions<TMessage> _options;
        private readonly IMessageQueue<TMessage> _sourceQueue;
        private readonly IMessageQueue<TMessage> _destinationQueue;
        private readonly IMessageReader<TMessage> _sourceReader;

        public Forwarder(ILogger<Forwarder<TMessage>> logger, IOptions<ForwarderOptions<TMessage>> options, IMessageQueue<TMessage> sourceQueue, IMessageQueue<TMessage> destinationQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _sourceQueue = sourceQueue ?? throw new ArgumentNullException(nameof(sourceQueue));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));

            var forwarderErrorHandler = _options.ForwardingErrorHandler ?? (_ => Task.FromResult(CompletionResult.Abandon));
            var startOptions = new MessageReaderStartOptions<TMessage>(new Handler<TMessage>(_logger, _destinationQueue, forwarderErrorHandler))
            {
                SubscriptionName = _options.SourceSubscriptionName,
                UserData = _options.SourceUserData
            };

            // feels bad man
            _sourceReader = _sourceQueue.GetReaderAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();
            _sourceReader.StartAsync(startOptions, default).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task<IMessageReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _destinationQueue.GetReaderAsync(cancellationToken);
        }

        public Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _sourceQueue.PostMessageAsync(message, cancellationToken);
        }

        public Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _sourceQueue.PostMessageAsync(message, attributes, cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Forwarder<TMessage>));
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
                _sourceReader.StopAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            _disposed = true;
        }

        ~Forwarder() => Dispose(false);

#if !NETSTANDARD2_0

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await _sourceReader.StopAsync(default).ConfigureAwait(false);

            Dispose(false);
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif

    }
}
