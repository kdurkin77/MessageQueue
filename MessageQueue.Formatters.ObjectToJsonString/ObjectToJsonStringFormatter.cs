using Newtonsoft.Json;
using System;

namespace KM.MessageQueue.Formatters.ObjectToJsonString
{
    public sealed class ObjectToJsonStringFormatter<TMessage> : IMessageFormatter<TMessage, string>
    {
        public string FormatMessage(TMessage message) =>
            JsonConvert.SerializeObject(message);

        public TMessage RevertMessage(string message) =>
            JsonConvert.DeserializeObject<TMessage>(message) 
                ?? throw new Exception($"Unable to convert bytes to type {typeof(TMessage)}");
    }
}
