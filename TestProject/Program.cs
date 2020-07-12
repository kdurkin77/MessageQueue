using KM.MessageQueue;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
                    .AddSingleton<MyApplication>()
                    .AddSingleton(typeof(IMessageFormatter<>), typeof(JsonFormatter<>))
                    .AddDiskMessageQueue<PersonMessage>(options =>
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

    public sealed class PersonMessage
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    public sealed class MyApplication
    {
        private readonly IMessageQueue<PersonMessage> _AzureTopic;

        public MyApplication(IMessageQueue<PersonMessage> azureTopic) => this._AzureTopic = azureTopic;

        public async Task RunAsync()
        {
            var msg = new PersonMessage()
            {
                Name = "name",
                Age = 99
            };

            var attributes = new MessageAttributes()
            {
                Label = "my-label",
                UserProperties = new Dictionary<string, object>()
                {
                    { "Key", string.Empty }
                }
            };

            await this._AzureTopic.PostMessageAsync(msg, attributes, default);
        }
    }
}
