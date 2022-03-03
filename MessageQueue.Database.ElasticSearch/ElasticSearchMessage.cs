using Newtonsoft.Json;
using System;

namespace KM.MessageQueue.Database.ElasticSearch
{
    internal sealed class ElasticSearchMessage
    {
        [JsonConstructor]
        public ElasticSearchMessage(MessageAttributes attributes)
        {
            MessageAttributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public MessageAttributes MessageAttributes { get; }
    }
}
