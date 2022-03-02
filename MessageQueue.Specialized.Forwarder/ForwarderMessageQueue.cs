using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class ForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1> : IMessageQueue<TMessageIn, TMessageOut1>
    {
        private bool _disposed = false;
        private readonly ILogger _logger;
        private readonly ForwarderMessageQueueOptions _options;
        private readonly IMessageQueue<TMessageIn, TMessageOut0> _sourceQueue;
        private readonly IMessageQueue<TMessageIn, TMessageOut1> _destinationQueue;
        private readonly IMessageQueueReader<TMessageIn, TMessageOut0> _sourceReader;

        public ForwarderMessageQueue(ILogger<ForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1>> logger, IOptions<ForwarderMessageQueueOptions> options, IMessageQueue<TMessageIn, TMessageOut0> sourceQueue, IMessageQueue<TMessageIn, TMessageOut1> destinationQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _sourceQueue = sourceQueue ?? throw new ArgumentNullException(nameof(sourceQueue));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));

            var forwarderErrorHandler = _options.ForwardingErrorHandler ?? (_ => Task.FromResult(CompletionResult.Abandon));
            var startOptions = new MessageQueueReaderStartOptions<TMessageIn, TMessageOut0>(new Handler<TMessageIn, TMessageOut0, TMessageOut1>(_logger, _destinationQueue, forwarderErrorHandler))
            {
                SubscriptionName = _options.SourceSubscriptionName,
                UserData = _options.SourceUserData
            };

            // feels bad man
            _sourceReader = _sourceQueue.GetReaderAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();
            _sourceReader.StartAsync(startOptions, default).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task<IMessageQueueReader<TMessageIn, TMessageOut1>> GetReaderAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _destinationQueue.GetReaderAsync(cancellationToken);
        }

        public Task PostMessageAsync(TMessageIn message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _sourceQueue.PostMessageAsync(message, cancellationToken);
        }

        public Task PostMessageAsync(TMessageIn message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _sourceQueue.PostMessageAsync(message, attributes, cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1>));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _sourceReader.StopAsync(default).ConfigureAwait(false).GetAwaiter().GetResult();

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

            await _sourceReader.StopAsync(default).ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif
    }
}
