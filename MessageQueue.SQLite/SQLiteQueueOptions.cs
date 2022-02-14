namespace KM.MessageQueue.SQLite
{
    public sealed class SQLiteQueueOptions
    {
        public string? ConnectionString { get; set; }
        public int? MaxQueueSize { get; set; }
    }
}
