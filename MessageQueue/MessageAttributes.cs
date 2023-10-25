using System.Collections.Generic;

namespace KM.MessageQueue
{
    /// <summary>
    /// Attributes for a Message in an <see cref="IMessageQueue{TMessage}"/>
    /// </summary>
    public sealed class MessageAttributes
    {
        public readonly Dictionary<string, object?> _attributes = new();

        /// <summary>
        /// The type of content
        /// </summary>
        public string? ContentType 
        {
            get
            {
                if (!_attributes.TryGetValue("ContentType", out var value))
                {
                    return null;
                }
                return value?.ToString();
            }
            set
            {
                _attributes["ContentType"] = value;
            }
        }

        /// <summary>
        /// The label, sometimes called topic or subscription
        /// </summary>
        public string? Label
        {
            get
            {
                if (!_attributes.TryGetValue("Label", out var value))
                {
                    return null;
                }
                return value?.ToString();
            }
            set
            {
                _attributes["Label"] = value;
            }
        }

        /// <summary>
        /// Any additional user properties
        /// </summary>
        public IDictionary<string, object>? UserProperties
        {
            get
            {
                if (!_attributes.TryGetValue("UserProperties", out var value))
                {
                    return null;
                }
                return (IDictionary<string, object>?)value;
            }
            set
            {
                _attributes["UserProperties"] = value;
            }
        }
    }
}
