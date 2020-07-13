using System;
using System.IO;

namespace KM.MessageQueue.FileSystem.Disk
{
    public sealed class DiskMessageQueueOptions<TMessage>
    {
        private DirectoryInfo? _MessageStore = null;

        // workaround until C#9 `init`
        public DirectoryInfo? MessageStore
        {
            get => _MessageStore;
            set
            {
                if (_MessageStore != null)
                {
                    throw new NotSupportedException();
                }

                _MessageStore = value;
            }
        }
    }
}
