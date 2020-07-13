using KM.MessageQueue;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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

    public sealed class MyApplication
    {
        private readonly IMessageQueue<MyMessage> _messageQueue;
        private readonly IMessageHandler<MyMessage> _handler;

        public MyApplication(IMessageQueue<MyMessage> messageQueue, IMessageHandler<MyMessage> handler)
        {
            _messageQueue = messageQueue;
            _handler = handler;
        }

        public async Task RunAsync(CancellationToken token)
        {
            var writerTask = Task.Run(() => WriteMessages(_messageQueue, token), token);

            await using var reader_1 = await _messageQueue.GetReaderAsync(token);
            await using var reader_2 = await _messageQueue.GetReaderAsync(token);

            await reader_1.StartAsync(_handler, "1", token);
            await reader_2.StartAsync(_handler, "2", token);

            await writerTask;

            Console.Write("press any key to exit");
            Console.ReadKey();

            await reader_1.StopAsync(token);
            await reader_2.StopAsync(token);
        }

        private static async Task WriteMessages(IMessageQueue<MyMessage> queue, CancellationToken token)
        {
            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine($"writing message: {i}");

                var msg = new MyMessage()
                {
                    Name = $"name-{i}",
                    Age = i
                };

                var attributes = new MessageAttributes()
                {
                    Label = $"my-label-{i}",
                    UserProperties = new Dictionary<string, object>()
                    {
                        { "Key", string.Empty }
                    }
                };

                await queue.PostMessageAsync(msg, attributes, token);

                await Task.Delay(500);
            }
        }
    }
}
