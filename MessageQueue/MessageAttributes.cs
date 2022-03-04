using System.Collections.Generic;

namespace KM.MessageQueue
{
    /// <summary>
    /// Attributes for a Message in an <see cref="IMessageQueue{TMessage}"/>
    /// </summary>
    public sealed class MessageAttributes
    {
        /// <summary>
        /// The type of content
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// The label, sometimes called topic or subscription
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Any additional user properties
        /// </summary>
        public IDictionary<string, object>? UserProperties { get; set; }
    }
}
