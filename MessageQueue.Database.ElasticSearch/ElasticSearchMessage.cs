using Newtonsoft.Json;
using System;

namespace KM.MessageQueue.Database.ElasticSearch
{
    [method: JsonConstructor]
    internal sealed class ElasticSearchMessage(MessageAttributes attributes)
    {
        public MessageAttributes MessageAttributes { get; } = attributes ?? throw new ArgumentNullException(nameof(attributes));
    }
}
