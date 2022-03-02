using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    internal sealed class Handler<TMessageIn, TMessageOut0, TMessageOut1> : IMessageHandler<TMessageIn, TMessageOut0>
    {
        private readonly ILogger _logger;
        private readonly IMessageQueue<TMessageIn, TMessageOut1> _destinationQueue;
        private readonly Func<Exception, Task<CompletionResult>> _forwarderErrorHandler;

        public Handler(ILogger logger, IMessageQueue<TMessageIn, TMessageOut1> destinationQueue, Func<Exception, Task<CompletionResult>> forwarderErrorHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _destinationQueue = destinationQueue ?? throw new ArgumentNullException(nameof(destinationQueue));
            _forwarderErrorHandler = forwarderErrorHandler ?? throw new ArgumentNullException(nameof(forwarderErrorHandler));
        }

        public Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken)
        {
            _logger.LogError(error, $"Error in {nameof(Handler<TMessageIn, TMessageOut0, TMessageOut1>)}");
            _forwarderErrorHandler(error);
            return Task.CompletedTask;
        }

        public async Task<CompletionResult> HandleMessageAsync(IMessageFormatter<TMessageIn, TMessageOut0> formatter, TMessageOut0 formattedMessage, MessageAttributes attributes, object? userData, CancellationToken cancellationToken)
        {
            try
            {
                var message = formatter.RevertMessage(formattedMessage);
                await _destinationQueue.PostMessageAsync(message, attributes, cancellationToken).ConfigureAwait(false);
                return CompletionResult.Complete;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure posting to {nameof(Handler<TMessageIn, TMessageOut0, TMessageOut1>)} destination queue");
                return await _forwarderErrorHandler(ex);
            }
        }
    }
}
