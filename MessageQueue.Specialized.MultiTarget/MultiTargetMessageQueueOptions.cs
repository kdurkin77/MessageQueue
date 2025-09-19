using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.MultiTarget
{
    /// <summary>
    /// Options for <see cref="MultiTargetMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class MultiTargetMessageQueueOptions<TMessage>
    {
        internal List<(IMessageQueue<TMessage> MessageQueue, Func<IMessageQueue<TMessage>, TMessage, Task<bool>> Predicate)> _targets = new();
        internal string? Name { get; set; }

        /// <summary>
        /// Action to take if the message isn't handled due to the target queue not being found
        /// </summary>
        public Action<TMessage>? OnUnhandledMessage { get; set; }

        /// <summary>
        /// Adds a target queue and it's predicate to select it
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MultiTargetMessageQueueOptions<TMessage> AddTarget(IMessageQueue<TMessage> messageQueue, Func<IMessageQueue<TMessage>, TMessage, bool> predicate)
        {
            if (messageQueue is null)
            {
                throw new ArgumentNullException(nameof(messageQueue));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            _targets.Add((messageQueue, (messageQueue, message) =>
            {
                var result = predicate(messageQueue, message);
                return Task.FromResult(result);
            }
            ));

            return this;
        }

        /// <summary>
        /// Adds a target queue and it's predicate to select it
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MultiTargetMessageQueueOptions<TMessage> AddTarget(IMessageQueue<TMessage> messageQueue, Func<IMessageQueue<TMessage>, TMessage, Task<bool>> predicate)
        {
            if (messageQueue is null)
            {
                throw new ArgumentNullException(nameof(messageQueue));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            _targets.Add((messageQueue, predicate));

            return this;
        }
    }
}
