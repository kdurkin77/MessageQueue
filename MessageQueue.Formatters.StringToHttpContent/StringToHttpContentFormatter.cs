using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KM.MessageQueue.Formatters.ObjectToJsonObject
{
    public sealed class StringToHttpContentFormatter : IMessageFormatter<string, HttpContent>
    {
        public Task<HttpContent> FormatMessage(string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return Task.FromResult((HttpContent)new StringContent(message, Encoding.UTF8, "application/json"));
        }

        public async Task<string> RevertMessage(HttpContent message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            return await message.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
