using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace KM.MessageQueue.Database.ElasticSearch
{
    internal sealed class ElasticSearchMessage
    {
        [JsonConstructor]
        public ElasticSearchMessage(MessageAttributes attributes, JObject? body)
        {
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public MessageAttributes Attributes { get; }
        public JObject Body { get; set; }
    }
}
