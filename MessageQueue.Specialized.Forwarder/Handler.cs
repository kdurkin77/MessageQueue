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

        public Handler(ILogger logger, IMessageQueue<TMessage> destinationQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));
        }

        public Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken)
        {
            _logger.LogError(error, $"Error in {nameof(Handler<TMessage>)}");
            return Task.CompletedTask;
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
                return CompletionResult.Failure;
            }
        }
    }
}
