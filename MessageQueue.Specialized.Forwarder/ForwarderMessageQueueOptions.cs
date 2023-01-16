using System;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class ForwarderMessageQueueOptions
    {
        public string? Name { get; set; }
        public string? SourceSubscriptionName { get; set; }
        public object? SourceUserData { get; set; }
        public TimeSpan? RetryDelay { get; set; }
        public Func<Exception, Task<CompletionResult>>? ForwardingErrorHandler { get; set; }
        public bool? DisposeSourceQueue { get; set; }
        public bool? DisposeDestinationQueue { get; set; }
    }
}
