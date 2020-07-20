using System;
using System.Threading.Tasks;

namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class ForwarderOptions<TMessage>
    {
        public string? SourceSubscriptionName { get; set; }
        public object? SourceUserData { get; set; }
        public Func<Exception, Task<CompletionResult>>? ForwardingErrorHandler { get; set; }
    }
}
