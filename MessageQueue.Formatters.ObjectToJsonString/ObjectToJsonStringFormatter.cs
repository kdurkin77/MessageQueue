using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace KM.MessageQueue.Formatters.ObjectToJsonString
{
    public sealed class ObjectToJsonStringFormatter<TMessage> : IMessageFormatter<TMessage, string>
    {
        public Task<string> FormatMessage(TMessage message) =>
            Task.FromResult(JsonConvert.SerializeObject(message));

        public Task<TMessage> RevertMessage(string message) =>
            Task.FromResult(JsonConvert.DeserializeObject<TMessage>(message) 
                ?? throw new Exception($"Unable to convert bytes to type {typeof(TMessage)}"));
    }
}
