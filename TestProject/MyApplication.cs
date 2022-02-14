using KM.MessageQueue;
using System;
using System.Collections.Generic;
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
            //await using var reader_2 = await _messageQueue.GetReaderAsync(token);

            var start_options_1 = new MessageQueueReaderStartOptions<MyMessage>(_handler)
            {
                //UserData = "1",
                SubscriptionName = "YOUR SUBSCRIPTION NAME HERE"
            };
            await reader_1.StartAsync(start_options_1, token);

            //var start_options_2 = new MessageReaderStartOptions<MyMessage>(_handler)
            //{
            //    UserData = "2",
            //    SubscriptionName = "YOUR SUBSCRIPTION NAME HERE"
            //};
            //await reader_2.StartAsync(start_options_2, token);

            await writerTask;

            Console.Write("press any key to exit");
            Console.ReadKey();

            await reader_1.StopAsync(token);
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

                await Task.Delay(500);
            }
        }
    }
}
