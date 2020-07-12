using System;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessage
    {
        public Guid Id { get; set; }
        public long SequenceNumber { get; set; }
        public MessageAttributes? Attributes { get; set; }
        public byte[]? Body { get; set; }
    }
}
