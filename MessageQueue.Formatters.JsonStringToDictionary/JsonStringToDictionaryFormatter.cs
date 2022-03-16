using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KM.MessageQueue.Formatters.JsonStringToDictionary
{
    public sealed class JsonStringToDictionary : IMessageFormatter<string, IDictionary<string, string>>
    {
        public Task<IDictionary<string, string>> FormatMessage(string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(JsonConvert.DeserializeObject<IDictionary<string, string>>(message)
                ?? throw new Exception($"Unable to convert string to dictionary")); ;
        }

        public Task<string> RevertMessage(IDictionary<string, string> message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult(JsonConvert.SerializeObject(message));
        }
    }
}
