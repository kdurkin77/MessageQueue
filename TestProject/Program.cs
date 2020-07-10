using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            try
            {
                var services =
                    new ServiceCollection()
                    .AddSingleton<Test>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(JsonFormatter<>))
                    .AddScoped(typeof(IMessageQueue<>), typeof(AzureTopic<>))
                    .Configure<AzureTopicOptions<string>>(options =>
                    {
                        options.Endpoint = "YOUR ENDPOINT HERE";
                        options.EntityPath = "YOUR ENTITY PATH HERE";
                        options.SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE";
                        options.SharedAccessKey = "YOUR SHARED ACCESS KEY HERE";
                    });

                var test = services.BuildServiceProvider().GetRequiredService<Test>();
                await test.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception - {ex}");
            }
        }
    }

    public class JsonFormatter<TMessage> : IMessageFormatter<TMessage>
    {
        public byte[] Format(TMessage message) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
    }

    public class Test
    {
        private readonly IMessageQueue<string> _AzureTopic;
        public Test(IMessageQueue<string> azureTopic) => this._AzureTopic = azureTopic;

        public async Task RunAsync() => await this._AzureTopic.PostMessageAsync("Test", default);
    }
}
