using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using KM.MessageQueue.FileSystem.Disk;
//using KM.MessageQueue.Formatters.ObjectToJsonObject;
//using KM.MessageQueue.Formatters.ObjectToJsonString;
//using KM.MessageQueue.Formatters.StringToBytes;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Client.Options;
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
                    .AddSingleton<IMessageHandler<MyMessage>, MyMessageHandler>()

                    //Azure queue
                    .AddAzureTopicMessageQueue<MyMessage>(options =>
                    {
                        options.UseConnectionStringBuilder(
                            endpoint: "YOUR ENDPOINT HERE",
                            entityPath: "YOUR ENTITY PATH HERE",
                            sharedAccessKeyName: "YOUR SHARED ACCESS KEY NAME HERE",
                            sharedAccessKey: "YOUR SHARED ACCESS KEY HERE"
                            //configure any service bus client options here
                            //options =>
                            //{
                            //    options.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpTcp;
                            //}
                        );
                        //to use your own formatter
                        //options.MessageFormatter = new JsonStringFormatter<MyMessage>().Compose(new StringToBytesFormatter());
                    })


                    //elasticsearch
                    .AddElasticSearchMessageQueue<MyMessage>(options =>
                    {
                        options.UseConnectionUri(new Uri("YOUR URI HERE"), settings =>
                        {
                            settings.BasicAuthentication("USERNAME", "PASSWORD");
                            settings.ThrowExceptions();
                        });
                        //to use your own formatter
                        //options.MessageFormatter = new JsonObjectFormatter<MyMessage>();
                    })


                    //Sqlite
                    .AddSqliteMessageQueue<MyMessage>(options =>
                    {
                        var path = Path.Combine(AppContext.BaseDirectory, "Queue.db");
                        options.ConnectionString = $"Data Source = {path}";
                        //to use your own formatter
                        //options.MessageFormatter = new JsonStringFormatter<MyMessage>();
                    })


                    //disk
                    .AddDiskMessageQueue<MyMessage>(options =>
                    {
                        options.MessageStore = new DirectoryInfo("/my-messages");
                        //to use your own formatter
                        //options.MessageFormatter = new JsonObjectFormatter<MyMessage>();
                    })
                    

                    //MQTT
                    .AddMqttMessageQueue<MyMessage>(options =>
                    {
                        options.UseManagedMqttClientOptionsBuilder(opts =>
                            opts
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                            .WithClientOptions(new MqttClientOptionsBuilder()
                                .WithTcpServer("HOST HERE")
                                .WithCredentials("USERNAME", "PASSWORD")
                                .Build())
                        );
                        //to handle building messages differently
                        //options.UseMessageBuilder(builder =>
                        //    builder
                        //    .WithExactlyOnceQoS()
                        //    .WithRetainFlag()
                        //    );
                        //to use your own formatter
                        //options.MessageFormatter = new JsonStringFormatter<MyMessage>().Compose(new StringToBytesFormatter());
                    })

                    
                    .AddForwarderMessageQueue<MyMessage, DiskMessageQueue<MyMessage>, AzureTopicMessageQueue<MyMessage>>((services, options) =>
                    {
                        var logger = services.GetRequiredService<ILogger<ForwarderMessageQueue<MyMessage>>>();
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
