using MessageQueue;
using MessageQueue.AzureTopic;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace TestProject
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            try
            {
                var service =
                    new ServiceCollection()
                    .AddSingleton<Test>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(MyFormatter<>))
                    .AddScoped(typeof(IMessageQueue<>), typeof(AzureTopic<>))
                    .Configure<AzureTopicMessageQueueOptions<string>>(options =>
                    {
                        options.Endpoint = "YOUR ENDPOINT HERE";
                        options.EntityPath = "YOUR ENTITY PATH HERE";
                        options.SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE";
                        options.SharedAccessKey = "YOUR SHARED ACCESS KEY HERE";
                    });

                var test = service.BuildServiceProvider().GetRequiredService<Test>();
                await test.RunAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception - {ex.Message}");
            }            
        }
    }
}
