using Newtonsoft.Json.Linq;
using System;

namespace KM.MessageQueue.Formatters.ObjectToJsonObject
{
    public sealed class ObjectToJsonObjectFormatter<TMessage> : IMessageFormatter<TMessage, JObject>
    {
        public JObject FormatMessage(TMessage message)
        {
            if(message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return JObject.FromObject(message);
        }

        public TMessage RevertMessage(JObject message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return message.ToObject<TMessage>()
                ?? throw new Exception($"Unable to convert JObject to type {typeof(TMessage)}"); ;
        }
    }
}
