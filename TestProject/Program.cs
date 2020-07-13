using KM.MessageQueue;
//using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
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
                    .AddSingleton<MyApplication>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(JsonFormatter<>))
                    .AddSingleton<IMessageHandler<MyMessage>, MyMessageHandler>()
                    .AddDiskMessageQueue<MyMessage>(options =>
                    {
                        options.MessageStore = new DirectoryInfo("/my-messages");
                    })
                    //.AddAzureTopicMessageQueue<PersonMessage>(options =>
                    //{
                    //    options.Endpoint = "YOUR ENDPOINT HERE";
                    //    options.EntityPath = "YOUR ENTITY PATH HERE";
                    //    options.SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE";
                    //    options.SharedAccessKey = "YOUR SHARED ACCESS KEY HERE";
                    //})
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
