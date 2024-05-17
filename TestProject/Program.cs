using Elastic.Transport;
using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using KM.MessageQueue.FileSystem.Disk;
//using KM.MessageQueue.Formatters.ObjectToJsonObject;
//using KM.MessageQueue.Formatters.ObjectToJsonString;
//using KM.MessageQueue.Formatters.StringToBytes;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
//using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
                        options
                            .AddConsole()
                            .SetMinimumLevel(LogLevel.Trace);
                    })
                    .AddSingleton<MyApplication>()

                    //Azure queue
                    //.AddAzureTopicMessageQueue<MyMessage>(options =>
                    //{
                    //    options
                    //        .UseConnectionStringBuilder(
                    //            endpoint: "YOUR ENDPOINT HERE",
                    //            entityPath: "YOUR ENTITY PATH HERE",
                    //            sharedAccessKeyName: "YOUR SHARED ACCESS KEY NAME HERE",
                    //            sharedAccessKey: "YOUR SHARED ACCESS KEY HERE"
                    //        //configure any service bus client options here
                    //        //options =>
                    //        //{
                    //        //    options.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpTcp;
                    //        //}
                    //        );
                    //    //to use your own formatter
                    //    //options.MessageFormatter = new ObjectToJsonStringFormatter<MyMessage>().Compose(new StringToBytesFormatter());
                    //    //in order to read more than 1 message at a time, set MaxReadCount to however many you want to read max. default is 1
                    //    //options.MaxReadCount = 1;
                    //    //in order to write more than 1 message at a time, set MaxWriteCount to however many you want to write max. default is 1
                    //    //options.MaxWriteCount = 1;
                    //})


                    //elasticsearch
                    //.AddElasticSearchMessageQueue<MyMessage>(options =>
                    //{
                    //    options
                    //        .UseConnectionUri(new Uri("YOUR URI HERE"), settings =>
                    //        {
                    //            settings
                    //                .Authentication(new BasicAuthentication("USERNAME", "PASSWORD"))
                    //                .ThrowExceptions();
                    //        });
                    //    //to use your own formatter
                    //    //options.MessageFormatter = new ObjectToJsonObjectFormatter<MyMessage>();
                    //    //in order to write more than 1 message at a time, set MaxWriteCount to however many you want to write max. default is 1
                    //    //options.MaxWriteCount = 1;
                    //})


                    //Sqlite
                    //.AddSqliteMessageQueue<MyMessage>(options =>
                    //{
                    //    var path = Path.Combine(AppContext.BaseDirectory, "Queue.db");
                    //    options.ConnectionString = $"Data Source = {path}";
                    //    //to increase the delay between checking messages when idle
                    //    //options.IdleDelay = TimeSpan.FromSeconds(200);
                    //    //to use your own formatter
                    //    //options.MessageFormatter = new ObjectToJsonStringFormatter<MyMessage>();
                    //    //in order to read more than 1 message at a time, set MaxReadCount to however many you want to read max. default is 1
                    //    options.MaxReadCount = 1;
                    //    //in order to write more than 1 message at a time, set MaxWriteCount to however many you want to write max. default is 1
                    //    options.MaxWriteCount = 1;
                    //})


                    //disk
                    //.AddDiskMessageQueue<MyMessage>(options =>
                    //{
                    //    options.MessageStore = new DirectoryInfo("./my-messages");
                    //    //to increase the delay between checking messages when idle
                    //    //options.IdleDelay = TimeSpan.FromSeconds(200);
                    //    //to use your own formatter
                    //    //options.MessageFormatter = new ObjectToJsonObjectFormatter<MyMessage>();
                    //})

                    .AddSingleton<IMessageQueue<MyMessage>>(services =>
                    {
                        var diskLogger = services.GetRequiredService<ILogger<DiskMessageQueue<MyMessage>>>();

                        var disk1 =
                            new DiskMessageQueue<MyMessage>(diskLogger, Options.Create(new DiskMessageQueueOptions<MyMessage>()
                            {
                                MessageStore = new("./my-messages-1")
                            }));

                        var disk2 =
                            new DiskMessageQueue<MyMessage>(diskLogger, Options.Create(new DiskMessageQueueOptions<MyMessage>()
                            {
                                MessageStore = new("./my-messages-2")
                            }));

                        var disk3 =
                            new DiskMessageQueue<MyMessage>(diskLogger, Options.Create(new DiskMessageQueueOptions<MyMessage>()
                            {
                                MessageStore = new("./my-messages-3")
                            }));

                        var forwarderLogger = services.GetRequiredService<ILogger<ForwarderMessageQueue<MyMessage>>>();

                        var forwarderOptions =
                            new ForwarderMessageQueueOptions()
                            {
                                DisposeSourceQueue = true
                            };

                        var forwarder =
                            new ForwarderMessageQueue<MyMessage>(forwarderLogger, Options.Create(forwarderOptions),
                                new ForwarderMessageQueue<MyMessage>(forwarderLogger, Options.Create(new ForwarderMessageQueueOptions()), disk1, disk2),
                                disk3
                                );

                        return forwarder;
                    })


                    //MQTT
                    //.AddMqttMessageQueue<MyMessage>(options =>
                    //{
                    //    options
                    //        .UseClientOptionsBuilder(opts =>
                    //            opts
                    //            .WithTcpServer("HOST HERE")
                    //            .WithCredentials("USERNAME", "PASSWORD")
                    //            .Build()
                    //        );
                    //    //to handle building messages differently
                    //    //.UseMessageBuilder(builder =>
                    //    //    builder
                    //    //    .WithExactlyOnceQoS()
                    //    //    .WithRetainFlag()
                    //    //);
                    //    //to use your own formatter
                    //    //options.MessageFormatter = new ObjectToJsonStringFormatter<MyMessage>().Compose(new StringToBytesFormatter());
                    //    //in order to read more than 1 message at a time, set MaxReadCount to however many you want to read max. default is 1
                    //    //options.MaxReadCount = 1;
                    //})


                    //Http
                    //.AddHttpMessageQueue<MyMessage>(options =>
                    //{
                    //    options.Uri = new Uri("https://raptordataapidev.ctdi.com/StbEngineering/InsertException");
                    //    options.Method = System.Net.Http.HttpMethod.Post;
                    //    //to put the message in the body of the request
                    //    //can also pass a formatter here to use your own
                    //    options.UseBody();
                    //    //to put the message in the query parameters
                    //    //can also pass a formatter here to use your own
                    //    //options.UseQueryParameters();
                    //    //to check the response a custom way
                    //    //options.CheckHttpResponse = message =>
                    //    //{
                    //    //    if (message is null)
                    //    //    {
                    //    //        throw new Exception("No response");
                    //    //    }

                    //    //    message.EnsureSuccessStatusCode();
                    //    //};
                    //    //Any headers you may need
                    //    //options.Headers = new Dictionary<string, string>()
                    //    //{
                    //    //    {"Authorization", "Basic Username:Password" }
                    //    //};
                    //})


                    //Forwarder example that sends to disk and then forwards to azure
                    //.AddForwarderMessageQueue<MyMessage, DiskMessageQueue<MyMessage>, AzureTopicMessageQueue<MyMessage>>((services, options) =>
                    //{
                    //    var logger = services.GetRequiredService<ILogger<ForwarderMessageQueue<MyMessage>>>();
                    //    options.SourceSubscriptionName = "YOUR SUBSCRIPTION NAME HERE";
                    //    options.ForwardingErrorHandler = ex =>
                    //    {
                    //        logger.LogError(ex, string.Empty);
                    //        return Task.FromResult(CompletionResult.Abandon);
                    //    };
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
