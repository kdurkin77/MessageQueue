using System.Collections.Generic;

namespace KM.MessageQueue
{
    public sealed class MessageAttributes
    {
        public string ContentType { get; set; }
        public string Label { get; set; }
        public IDictionary<string, object> UserProperties { get; set; }
    }
}
