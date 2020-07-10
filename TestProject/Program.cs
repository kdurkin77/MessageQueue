using KM.MessageQueue;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public static class Program
    {
        public static async Task Main(string[] _)
        {
            try
            {
                var services = new ServiceCollection()
                    .AddSingleton<Test>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(JsonFormatter<>))
                    .AddAzureTopicMessageQueue<MessageRecord>(options =>
                    {
                        options.Endpoint = "YOUR ENDPOINT HERE";
                        options.EntityPath = "YOUR ENTITY PATH HERE";
                        options.SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE";
                        options.SharedAccessKey = "YOUR SHARED ACCESS KEY HERE";
                    })
                    .BuildServiceProvider();

                var test = services.GetRequiredService<Test>();
                await test.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception - {ex}");
            }
        }
    }

    public sealed class JsonFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] Format(TMessage message) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }

    public sealed class MessageRecord
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public sealed class Test
    {
        private readonly IMessageQueue<MessageRecord> _AzureTopic;

        public Test(IMessageQueue<MessageRecord> azureTopic) => this._AzureTopic = azureTopic;

        public async Task RunAsync()
        {
            var msg = new MessageRecord()
            {
                Name = "name",
                Age = 99
            };

            await this._AzureTopic.PostMessageAsync(msg, default);
        }
    }
}
