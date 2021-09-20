using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using KM.MessageQueue.FileSystem.Disk;
using KM.MessageQueue.Formatters.Json;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
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
                    .AddLogging(options =>
                    {
                        options.AddConsole();
                        options.SetMinimumLevel(LogLevel.Trace);
                    })
                    .AddSingleton<MyApplication>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(JsonFormatter<>))
                    .AddSingleton<IMessageHandler<MyMessage>, MyMessageHandler>()
                    .AddDiskMessageQueue<MyMessage>(options =>
                    {
                        options.MessageStore = new DirectoryInfo("/my-messages");
                    })
                    .AddAzureTopicMessageQueue<MyMessage>(options =>
                    {
                        options.Endpoint = "YOUR ENDPOINT HERE";
                        options.EntityPath = "YOUR ENTITY PATH HERE";
                        options.SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE";
                        options.SharedAccessKey = "YOUR SHARED ACCESS KEY HERE";
                    })
                    .AddForwarder<MyMessage, DiskMessageQueue<MyMessage>, AzureTopic<MyMessage>>((services, options) =>
                    {
                        var logger = services.GetRequiredService<ILogger<Forwarder<MyMessage>>>();
                        options.SourceSubscriptionName = "YOUR SUBSCRIPTION NAME HERE";
                        options.ForwardingErrorHandler = ex =>
                        {
                            logger.LogError(ex, string.Empty);
                            return Task.FromResult(CompletionResult.Abandon);
                        };
                    })
                    .BuildServiceProvider();

                var test = services.GetRequiredService<MyApplication>();
                await test.RunAsync(default);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception - {ex}");
            }
        }
    }
}
