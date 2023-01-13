using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    internal sealed class Handler<TMessage> : IMessageHandler<TMessage>
    {
        private readonly ILogger _logger;
        private readonly IMessageQueue<TMessage> _destinationQueue;
        private readonly TimeSpan? _retryDelay;
        private readonly Func<Exception, Task<CompletionResult>> _forwarderErrorHandler;

        public Handler(ILogger logger, IMessageQueue<TMessage> destinationQueue, TimeSpan? retryDelay, Func<Exception, Task<CompletionResult>> forwarderErrorHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));
            _retryDelay = retryDelay;
            _forwarderErrorHandler = forwarderErrorHandler ?? throw new ArgumentNullException(nameof(forwarderErrorHandler));
        }

        public async Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken)
        {
            _logger.LogError(error, $"Error in {nameof(Handler<TMessage>)}");
            await _forwarderErrorHandler(error);
        }

        public async Task<CompletionResult> HandleMessageAsync(TMessage message, MessageAttributes attributes, object? userData, CancellationToken cancellationToken)
        {
            try
            {
                await _destinationQueue.PostMessageAsync(message, attributes, cancellationToken).ConfigureAwait(false);
                return CompletionResult.Complete;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure posting to {nameof(Handler<TMessage>)} destination queue");
                var result = await _forwarderErrorHandler(ex).ConfigureAwait(false);
                if (_retryDelay is not null)
                {
                    await Task.Delay(_retryDelay.Value, cancellationToken).ConfigureAwait(false);
                }
                return result;
            }
        }
    }
}
