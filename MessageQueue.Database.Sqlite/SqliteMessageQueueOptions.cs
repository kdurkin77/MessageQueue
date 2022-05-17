using System;

namespace KM.MessageQueue.Database.Sqlite
{
    /// <summary>
    /// Options for <see cref="SqliteMessageQueue{TMessage}"/>
    /// </summary>
    public sealed class SqliteMessageQueueOptions<TMessage>
    {
        /// <summary>
        /// Connection string for the database. The table will be created for you if it does not already exist
        /// </summary>
        public string? ConnectionString { get; set; }
        /// <summary>
        /// Max queue size
        /// </summary>
        public int? MaxQueueSize { get; set; }
        /// <summary>
        /// Delay before rechecking for messages in the reader if there weren't any before
        /// </summary>
        public TimeSpan? IdleDelay { get; set; }
        /// <summary>
        /// The <see cref="IMessageFormatter{TMessageIn, TMessageOut}"/> to use. If not specified, it will use the default
        /// formatter which serializes the message to a json string />
        /// </summary>
        public IMessageFormatter<TMessage, string>? MessageFormatter { get; set; }
    }
}
