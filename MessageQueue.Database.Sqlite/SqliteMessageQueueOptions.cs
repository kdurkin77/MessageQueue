namespace KM.MessageQueue.Database.Sqlite
{
    public sealed class SqliteMessageQueueOptions
    {
        public string? ConnectionString { get; set; }
        public int? MaxQueueSize { get; set; }
    }
}
