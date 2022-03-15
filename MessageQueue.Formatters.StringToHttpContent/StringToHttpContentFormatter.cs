using System;
using System.Net.Http;

namespace KM.MessageQueue.Formatters.ObjectToJsonObject
{
    public sealed class StringToHttpContentFormatter : IMessageFormatter<string, HttpContent>
    {
        public HttpContent FormatMessage(string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return new StringContent(message);
        }

        public string RevertMessage(HttpContent message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return message.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
