using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace KM.MessageQueue.SQLite
{
    internal class SQLiteDatabaseContext : DbContext
    {
        public DbSet<SQLiteQueueMessage> SQLiteQueueMessages => Set<SQLiteQueueMessage>();

        private readonly string _ConnectionString;

        public SQLiteDatabaseContext(string connectionString)
        {
            _ConnectionString = connectionString;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(_ConnectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SQLiteQueueMessage>().HasIndex(b => b.SequenceNumber);
            base.OnModelCreating(modelBuilder);
        }
    }

    internal sealed class SQLiteQueueMessage
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public long SequenceNumber { get; set; }
        [Required]
        public string Attributes { get; set; } = null!;
        [Required]
        public byte[] Body { get; set; } = null!;
    }
}
