using KM.MessageQueue;
using Newtonsoft.Json;
using System.Text;

namespace TestProject
{
    public sealed class JsonFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] MessageToBytes(TMessage message) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        public TMessage BytesToMessage(byte[] bytes) =>
            JsonConvert.DeserializeObject<TMessage>(Encoding.UTF8.GetString(bytes));
    }
}
