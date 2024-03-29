﻿using System.Threading.Tasks;

namespace KM.MessageQueue
{
    /// <summary>
    /// A formatter interface for an <see cref="IMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessageIn"></typeparam>
    /// <typeparam name="TMessageOut"></typeparam>
    public interface IMessageFormatter<TMessageIn, TMessageOut>
    {
        /// <summary>
        /// Converts a message from <typeparamref name="TMessageOut"/> to <typeparamref name="TMessageIn"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<TMessageIn> RevertMessage(TMessageOut message);

        /// <summary>
        /// Converts a message from <typeparamref name="TMessageIn"/> to <typeparamref name="TMessageOut"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<TMessageOut> FormatMessage(TMessageIn message);
    }
}
