using KM.MessageQueue;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public sealed class MyMessage
    {
        public Guid? Id { get; set; }
        public string? Test { get; set; }
    }

    public sealed class MyApplication
    {
        private readonly IMessageQueue<MyMessage> _messageQueue;

        public MyApplication(IMessageQueue<MyMessage> messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public async Task RunAsync(CancellationToken token)
        {
            //for many messages to be written at once see function WriteManyMessages
            var writerTask = Task.Run(() => WriteMessages(_messageQueue, token), token);

            var reader_options_1 = new MessageQueueReaderOptions<MyMessage>()
            {
                //UserData = "1",
                SubscriptionName = "YOUR SUBSCRIPTION NAME HERE",
                //set the count you want to read at once here and then use TryReadManyMessagesAsync to read more than 1
                ReadCount = 1
            };

            await using var reader_1 = await _messageQueue.GetReaderAsync(reader_options_1, token);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            int count = 0;
            bool loop;
            do
            {
                (loop, _) = await reader_1.TryReadMessageAsync(
                    (msg, _, _, _) =>
                    {
                        Console.WriteLine($"Read [{++count}]: {msg.Id} {msg.Test}");
                        return Task.FromResult(CompletionResult.Complete);
                    }, cts.Token);

                if (!loop)
                {
                    Console.WriteLine("Exiting reader loop");
                }

            } while (loop);

            //read many messages reader loop
            //int count = 0;
            //bool loop;
            //do
            //{
            //    (loop, _) = await reader_1.TryReadManyMessagesAsync(
            //        (msgs, _, _) =>
            //        {
            //            Console.WriteLine($"Read [{++count}]: {msgs.Count()} messages read");
            //            foreach (var (msg, attr) in msgs)
            //            {
            //                Console.WriteLine($"{msg}");
            //            }
            //            return Task.FromResult(CompletionResult.Complete);
            //        }, cts.Token);

            //    if (!loop)
            //    {
            //        Console.WriteLine("Exiting reader loop");
            //    }

            //} while (loop);


            await writerTask;

            _messageQueue.Dispose();

            Console.Write("press any key to exit");
            Console.ReadKey();

            //await reader_1.StopAsync(token);
        }

        private static async Task WriteMessages(IMessageQueue<MyMessage> queue, CancellationToken token)
        {
            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"writing message {i}");

                var msg = new MyMessage()
                {
                    Id = Guid.NewGuid(),
                    Test = "TEST"
                };

                var attributes = new MessageAttributes()
                {
                    Label = ""
                };

                await queue.PostMessageAsync(msg, attributes, token);
                await Task.Delay(500, token);
            }
        }

        private static async Task WriteManyMessages(IMessageQueue<MyMessage> queue, CancellationToken token)
        {
            var msgs =
                Enumerable.Range(1, 10)
                .Select(i =>
                {
                    var msg = new MyMessage()
                    {
                        Id = Guid.NewGuid(),
                        Test = "TEST"
                    };

                    var attributes = new MessageAttributes()
                    {
                        Label = ""
                    };
                    return (msg, attributes);
                })
                .ToList();

            await queue.PostManyMessagesAsync(msgs, token);
            await Task.Delay(500, token);
        }
    }
}
