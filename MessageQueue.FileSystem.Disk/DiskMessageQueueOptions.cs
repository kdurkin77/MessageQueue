using KM.MessageQueue.Formatters.ObjectToJsonObject;
using Newtonsoft.Json.Linq;
using System.IO;

namespace KM.MessageQueue.FileSystem.Disk
{
    /// <summary>
    /// Options for <see cref="DiskMessageQueue{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public sealed class DiskMessageQueueOptions<TMessage>
    {
        /// <summary>
        /// Max queue size, used to prevent running out of memory
        /// </summary>
        public int? MaxQueueSize { get; set; }
        /// <summary>
        /// Size to partition the messages, this is the number of messages in a file
        /// </summary>
        public int? MessagePartitionSize { get; set; }
        /// <summary>
        /// Where to store the messages
        /// </summary>
        public DirectoryInfo? MessageStore { get; set; }
        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which converts the message to a <see cref="JObject"/> />
        /// /// </summary>
        public IMessageFormatter<TMessage, JObject> MessageFormatter { get; set; } = new ObjectToJsonObjectFormatter<TMessage>();
    }
}
