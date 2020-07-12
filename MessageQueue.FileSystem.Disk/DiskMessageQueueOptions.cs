using System.IO;

namespace KM.MessageQueue.FileSystem.Disk
{
    public sealed class DiskMessageQueueOptions<TMessage>
    {
        public DirectoryInfo? MessageStore { get; set; }
    }
}
