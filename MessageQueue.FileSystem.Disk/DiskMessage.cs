using Newtonsoft.Json;
using System;

namespace KM.MessageQueue.FileSystem.Disk
{
    internal sealed class DiskMessage
    {
        [JsonConstructor]
        public DiskMessage(Guid id, long sequenceNumber, MessageAttributes attributes, byte[] body)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException($"Argument may not be empty", nameof(id));
            }

            if (sequenceNumber <= 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
            }

            Id = id;
            SequenceNumber = sequenceNumber;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }

        public Guid Id { get; }
        public long SequenceNumber { get; }
        public MessageAttributes Attributes { get; }
        public byte[] Body { get; set; }
    }
}
