using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KM.MessageQueue.Formatters.JsonStringToDictionary
{
    public sealed class JsonStringToDictionary : IMessageFormatter<string, IDictionary<string, string>>
    {
        public IDictionary<string, string> FormatMessage(string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return JsonConvert.DeserializeObject<IDictionary<string, string>>(message)
                ?? throw new Exception($"Unable to convert string to dictionary"); ;
        }

        public string RevertMessage(IDictionary<string, string> message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return JsonConvert.SerializeObject(message);
        }
    }
}
