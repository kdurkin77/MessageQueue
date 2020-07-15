namespace KM.MessageQueue.Specialized.Forwarder
{
    public sealed class ForwarderOptions<TMessage>
    {
        public string? SourceSubscriptionName { get; set; }
        public object? SourceUserData { get; set; }
    }
}
