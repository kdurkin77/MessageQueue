using System;

namespace KM.MessageQueue.Formatters.Specialized.Composition
{
    public sealed class CompositionFormatter<TMessageIn, TMessageIntermediate, TMessageOut> : IMessageFormatter<TMessageIn, TMessageOut>
    {
        private readonly IMessageFormatter<TMessageIn, TMessageIntermediate> _sourceFormatter;
        private readonly IMessageFormatter<TMessageIntermediate, TMessageOut> _destinationFormatter;

        public CompositionFormatter(IMessageFormatter<TMessageIn, TMessageIntermediate> sourceFormatter, IMessageFormatter<TMessageIntermediate, TMessageOut> destinationFormatter)
        {
            _sourceFormatter = sourceFormatter ?? throw new ArgumentNullException(nameof(sourceFormatter));
            _destinationFormatter = destinationFormatter ?? throw new ArgumentNullException(nameof(destinationFormatter));
        }

        public TMessageOut FormatMessage(TMessageIn message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var intermediateMessage = _sourceFormatter.FormatMessage(message);
            return _destinationFormatter.FormatMessage(intermediateMessage);
        }

        public TMessageIn RevertMessage(TMessageOut message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var intermediateMessage = _destinationFormatter.RevertMessage(message);
            return _sourceFormatter.RevertMessage(intermediateMessage);
        }
    }
}
