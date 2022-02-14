using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using KM.MessageQueue.FileSystem.Disk;
using KM.MessageQueue.Formatters.Json;
using KM.MessageQueue.Specialized.Forwarder;
//using KM.MessageQueue.Sqlite;
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
            var formatter = new JsonFormatter<MyMessage>();
            var diskOptions = new DiskMessageQueueOptions()
            {
                MessageStore = new DirectoryInfo("/my-messages")
            };
            var diskQueue = new DiskMessageQueue<MyMessage>(new Logger<DiskMessageQueue<MyMessage>>(), Options.Create(diskOptions), formatter);

            //sqlite queue could be used in place of disk queue also
            //var sqliteOptions = new SqliteMessageQueueOptions()
            //{
            //    ConnectionString = $"Data Source = {Path.Combine(AppContext.BaseDirectory, "Queue.db")}"
            //};
            //var sqliteQueue = new SqliteMessageQueue<MyMessage>(new Logger<SqliteMessageQueue<MyMessage>>, Options.Create(sqliteOptions), formatter);

            var azureTopicOptions = new AzureTopicMessageQueueOptions()
            {
                Endpoint = "YOUR ENDPOINT HERE",
                EntityPath = "YOUR ENTITY PATH HERE",
                SharedAccessKey = "YOUR SHARED ACCESS KEY HERE",
                SharedAccessKeyName = "YOUR SHARED ACCESS KEY NAME HERE"
            };
            var azureTopic = new AzureTopicMessageQueue<MyMessage>(new Logger<AzureTopicMessageQueue<MyMessage>>(), Options.Create(azureTopicOptions), formatter);

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

            var msg = new MyMessage()
            {
                GUID = Guid.NewGuid(),
                TEST = "TEST"
            };

            var attributes = new MessageAttributes()
            {
                Label = "YOUR LABEL HERE",
                ContentType = "application/json"
            };

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
