using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace KM.MessageQueue.Formatters.ObjectToJsonObject
{
    public sealed class ObjectToJsonObjectFormatter<TMessage> : IMessageFormatter<TMessage, JObject>
    {
        public Task<JObject> FormatMessage(TMessage message)
        {
            if(message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(JObject.FromObject(message));
        }

        public Task<TMessage> RevertMessage(JObject message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(message.ToObject<TMessage>()
                ?? throw new Exception($"Unable to convert JObject to type {typeof(TMessage)}"));
        }
    }
}
