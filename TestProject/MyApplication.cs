using KM.MessageQueue;
using System;
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
            var writerTask = Task.Run(() => WriteMessages(_messageQueue, token), token);

            var reader_options_1 = new MessageQueueReaderOptions<MyMessage>()
            {
                //UserData = "1",
                SubscriptionName = "YOUR SUBSCRIPTION NAME HERE"
            };

            await using var reader_1 = await _messageQueue.GetReaderAsync(reader_options_1, token);
            //await using var reader_2 = await _messageQueue.GetReaderAsync(token);

            //await reader_1.StartAsync(start_options_1, token);

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


            //var start_options_2 = new MessageReaderStartOptions<MyMessage>(_handler)
            //{
            //    UserData = "2",
            //    SubscriptionName = "YOUR SUBSCRIPTION NAME HERE"
            //};
            //await reader_2.StartAsync(start_options_2, token);

            await writerTask;

            _messageQueue.Dispose();

            Console.Write("press any key to exit");
            Console.ReadKey();

            //await reader_1.StopAsync(token);
            //await reader_2.StopAsync(token);
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
    }
}
