using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using KM.MessageQueue.Database.ElasticSearch;
using KM.MessageQueue.Database.Sqlite;
using KM.MessageQueue.FileSystem.Disk;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyTestProject
{
    public class Program
    {
        public static async Task Main(string[] _)
        {
            //elasticsearch setup example
            var elasticSearchOptions = 
                new ElasticSearchMessageQueueOptions<MyMessage>()
                .UseConnectionUri(new Uri("YOUR URI HERE"), 
                    (options) => 
                    { 
                        options.ThrowExceptions();
                    }
                );
            var elasticSearchQueue = new ElasticSearchMessageQueue<MyMessage>(new Logger<ElasticSearchMessageQueue<MyMessage>>(), Options.Create(elasticSearchOptions));
            
            //sqlite setup example
            var sqliteOptions = new SqliteMessageQueueOptions<MyMessage>()
            {
                ConnectionString = $"Data Source = {Path.Combine(AppContext.BaseDirectory, "Queue.db")}"
                //to increase the delay between checking messages when idle
                //IdleDelay = TimeSpan.FromMilliseconds(200)
            };
            var sqliteQueue = new SqliteMessageQueue<MyMessage>(new Logger<SqliteMessageQueue<MyMessage>>(), Options.Create(sqliteOptions));

            //setup for disk queue forwarding to azure queue
            var diskOptions = new DiskMessageQueueOptions<MyMessage>()
            {
                MessageStore = new DirectoryInfo("/my-messages")
                //to increase the delay between checking messages when idle
                //IdleDelay = TimeSpan.FromMilliseconds(200)
            };
            var diskQueue = new DiskMessageQueue<MyMessage>(new Logger<DiskMessageQueue<MyMessage>>(), Options.Create(diskOptions));

            var azureTopicOptions = 
                new AzureTopicMessageQueueOptions<MyMessage>()
                .UseConnectionStringBuilder(
                    endpoint:"YOUR ENDPOINT HERE", 
                    entityPath: "YOUR ENTITYPATH HERE", 
                    sharedAccessKeyName: "YOUR SHARED ACCESS KEY NAME HERE", 
                    sharedAccessKey: "YOUR SHARED ACCESS KEY HERE"
                );
            var azureTopic = new AzureTopicMessageQueue<MyMessage>(new Logger<AzureTopicMessageQueue<MyMessage>>(), Options.Create(azureTopicOptions));

            var forwarderLogger = new Logger<ForwarderMessageQueue<MyMessage>>();
            var forwarderOptions = new ForwarderMessageQueueOptions()
            {
                SourceSubscriptionName = "YOUR SUBSCRIPTION NAME HERE",
                ForwardingErrorHandler = (ex) =>
                {
                    forwarderLogger.LogError(ex, string.Empty);
                    return Task.FromResult(CompletionResult.Abandon);
                }
            };
            var forwarder = new ForwarderMessageQueue<MyMessage>(forwarderLogger, Options.Create(forwarderOptions), diskQueue, azureTopic);


            //create the message
            var msg = new MyMessage()
            {
                GUID = Guid.NewGuid(),
                TEST = "TEST"
            };

            //with attributes
            var attributes = new MessageAttributes()
            {
                Label = "YOUR LABEL HERE",
                ContentType = "application/json"
            };

            //and post to whichever queue you'd like. this one posts to the forwarder queue which posts to the disk and then forwards to azure
            await forwarder.PostMessageAsync(msg, attributes, CancellationToken.None);

            Console.Write("press any key to exit");
            Console.ReadKey();
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
