using KM.MessageQueue;
using Newtonsoft.Json;
using System;
using System.Text;

namespace MessageQueue.Formatters.Json
{
    public sealed class JsonFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] MessageToBytes(TMessage message) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        public TMessage BytesToMessage(byte[] bytes) =>
            JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(bytes)) 
                ?? throw new Exception($"Unable to convert bytes to type {typeof(TMessage)}");
    }
}
