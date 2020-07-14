using KM.MessageQueue;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject
{
    public sealed class MyMessageHandler : IMessageHandler<MyMessage>
    {
        public Task HandleErrorAsync(Exception error, object? userData, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Message error: {error}");
            return Task.CompletedTask;
        }

        public async Task<CompletionResult> HandleMessageAsync(MyMessage message, MessageAttributes attributes, object? userData, CancellationToken cancellationToken)
        {
            if (message is null || attributes is null)
            {
                return CompletionResult.Abandon;
            }

            Console.WriteLine($"reader {userData}: {JsonConvert.SerializeObject(message)}");

            await Task.CompletedTask;

            return CompletionResult.Complete;
        }
    }
}
