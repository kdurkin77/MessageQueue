using MessageQueue;
using Newtonsoft.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public class MyFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] Format(TMessage message) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }

    public class Test
    {
        private readonly IMessageQueue<string> _AzureTopic;
        public Test(IMessageQueue<string> azureTopic) => this._AzureTopic = azureTopic;

        public async Task RunAsync() => await this._AzureTopic.PostMessageAsync("Test", new CancellationToken());
    }
}
