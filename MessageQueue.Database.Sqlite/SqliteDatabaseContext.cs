using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KM.MessageQueue.Database.Sqlite
{
    internal sealed class SqliteDatabaseContext : DbContext
    {
        public DbSet<SqliteQueueMessage> SqliteQueueMessages => Set<SqliteQueueMessage>();

        public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SqliteQueueMessage>().HasIndex(b => b.SequenceNumber);
            base.OnModelCreating(modelBuilder);
        }
    }

    internal sealed class SqliteQueueMessage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid? Id { get; set; }
        [Required]
        public long? SequenceNumber { get; set; }
        [Required]
        public string? Attributes { get; set; }
        [Required]
        public string? Body { get; set; }
    }
}
