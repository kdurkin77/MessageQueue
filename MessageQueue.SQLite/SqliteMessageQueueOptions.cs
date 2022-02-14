namespace KM.MessageQueue.SQLite
{
    public sealed class SQLiteMessageQueueOptions
    {
        public string? ConnectionString { get; set; }
        public int? MaxQueueSize { get; set; }
    }
}
