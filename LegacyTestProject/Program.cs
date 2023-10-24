using KM.MessageQueue;
using KM.MessageQueue.Mqtt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyTestProject
{
    public class Program
    {
        internal sealed class StringToBytesNullableFormatter : IMessageFormatter<string, byte[]>
        {
            public Task<byte[]> FormatMessage(string message) =>
                Task.FromResult(Encoding.UTF8.GetBytes(message));

            public Task<string> RevertMessage(byte[] message)
            {
                if (message is null)
                {
                    return Task.FromResult(string.Empty);
                }
                return Task.FromResult(Encoding.UTF8.GetString(message));
            }
        }

        private static async Task ReaderLoop(IMessageQueue<string> queue, string subscription, CancellationTokenSource cts)
        {
            var readerOptions = new MessageQueueReaderOptions<string>()
            {
                SubscriptionName = subscription
            };

            var reader = await queue.GetReaderAsync(readerOptions, cts.Token).ConfigureAwait(false);
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    Task<CompletionResult> HandleMessage(string message, MessageAttributes attributes, object userData, CancellationToken cancellationToken)
                    {
                        try
                        {
                            Console.WriteLine($"Topic {attributes.Label} Received Message: {message}");
                            return Task.FromResult(CompletionResult.Complete);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            return Task.FromResult(CompletionResult.Abandon);
                        }
                    }

                    await reader.ReadMessageAsync(HandleMessage, cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) when (cts.IsCancellationRequested)
                {
                    // cancellation requested
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                }
            }
        }

        public static async Task Main(string[] _)
        {
            ////elasticsearch setup example
            //var elasticSearchOptions = 
            //    new ElasticSearchMessageQueueOptions<MyMessage>()
            //    .UseConnectionUri(new Uri("YOUR URI HERE"), 
            //        (options) => 
            //        { 
            //            options.ThrowExceptions();
            //        }
            //    );
            //var elasticSearchQueue = new ElasticSearchMessageQueue<MyMessage>(new Logger<ElasticSearchMessageQueue<MyMessage>>(), Options.Create(elasticSearchOptions));
            
            ////sqlite setup example
            //var sqliteOptions = new SqliteMessageQueueOptions<MyMessage>()
            //{
            //    ConnectionString = $"Data Source = {Path.Combine(AppContext.BaseDirectory, "Queue.db")}"
            //    //to increase the delay between checking messages when idle
            //    //IdleDelay = TimeSpan.FromMilliseconds(200)
            //};
            //var sqliteQueue = new SqliteMessageQueue<MyMessage>(new Logger<SqliteMessageQueue<MyMessage>>(), Options.Create(sqliteOptions));

            //mqtt setup example
            var mqttOptions = 
                new MqttMessageQueueOptions<string>()
                .UseClientOptionsBuilder(builder =>
                {
                    builder
                        .WithTcpServer("")
                        .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)//so it doesn't take from mem
                        .WithWillRetain(false)//so it doesn't take from mem
                        .WithCredentials("", "")
                        .Build();
                });
            mqttOptions.MessageFormatter = new StringToBytesNullableFormatter();
            var mqttQueue = new MqttMessageQueue<string>(new Logger<MqttMessageQueue<string>>(), Options.Create(mqttOptions));

            async Task RunPerDevice(string deviceId)
            {
                var cts = new CancellationTokenSource();
                var readerLoopTask = Task.Run(() => ReaderLoop(mqttQueue, $"/pi/{deviceId}/serial/in", cts));

                var messageAtt = new MessageAttributes() { Label = $"/pi/{deviceId}/serial/out" };
                await mqttQueue.PostMessageAsync("RebootNow\n", messageAtt, cts.Token);

                await Task.Delay(TimeSpan.FromMinutes(1));
                cts.Cancel();
                await readerLoopTask;
            }

            var devices = new List<string>()
                {
                    ""
                };

            await Task.WhenAll(devices.Select(device => RunPerDevice(device)));

            Console.WriteLine("COMPLETE - any key to exit");
            Console.ReadLine();

            //setup for disk queue forwarding to azure queue
            //var diskOptions = new DiskMessageQueueOptions<MyMessage>()
            //{
            //    MessageStore = new DirectoryInfo("/my-messages")
            //    //to increase the delay between checking messages when idle
            //    //IdleDelay = TimeSpan.FromMilliseconds(200)
            //};
            //var diskQueue = new DiskMessageQueue<MyMessage>(new Logger<DiskMessageQueue<MyMessage>>(), Options.Create(diskOptions));

            //var azureTopicOptions = 
            //    new AzureTopicMessageQueueOptions<MyMessage>()
            //    .UseConnectionStringBuilder(
            //        endpoint:"YOUR ENDPOINT HERE", 
            //        entityPath: "YOUR ENTITYPATH HERE", 
            //        sharedAccessKeyName: "YOUR SHARED ACCESS KEY NAME HERE", 
            //        sharedAccessKey: "YOUR SHARED ACCESS KEY HERE"
            //    );
            //var azureTopic = new AzureTopicMessageQueue<MyMessage>(new Logger<AzureTopicMessageQueue<MyMessage>>(), Options.Create(azureTopicOptions));

            //var forwarderLogger = new Logger<ForwarderMessageQueue<MyMessage>>();
            //var forwarderOptions = new ForwarderMessageQueueOptions()
            //{
            //    SourceSubscriptionName = "YOUR SUBSCRIPTION NAME HERE",
            //    ForwardingErrorHandler = (ex) =>
            //    {
            //        forwarderLogger.LogError(ex, string.Empty);
            //        return Task.FromResult(CompletionResult.Abandon);
            //    }
            //};
            //var forwarder = new ForwarderMessageQueue<MyMessage>(forwarderLogger, Options.Create(forwarderOptions), diskQueue, azureTopic);


            ////create the message
            //var msg = new MyMessage()
            //{
            //    GUID = Guid.NewGuid(),
            //    TEST = "TEST"
            //};

            ////with attributes
            //var attributes = new MessageAttributes()
            //{
            //    Label = "YOUR LABEL HERE",
            //    ContentType = "application/json"
            //};

            ////and post to whichever queue you'd like. this one posts to the forwarder queue which posts to the disk and then forwards to azure
            //await forwarder.PostMessageAsync(msg, attributes, CancellationToken.None);

            //Console.Write("press any key to exit");
            //Console.ReadKey();
        }
    }

    public sealed class MyMessage
    {
        public Guid GUID { get; set; }
        public string TEST { get; set; }
    }

    public class Logger<T> : ILogger<T>
    {
        private sealed class DummyScope : IDisposable
        {
            public void Dispose() { }
        }

        private static readonly DummyScope _dummyScope = new DummyScope();

        public IDisposable BeginScope<TState>(TState state)
        {
            return _dummyScope;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //log here
            Console.WriteLine($"Loglevel: {logLevel}; EventId: {eventId}; State: {state}; Exception: {exception};");
        }
    }
}
