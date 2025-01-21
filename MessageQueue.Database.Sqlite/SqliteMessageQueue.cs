using KM.MessageQueue.Formatters.ObjectToJsonString;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Database.Sqlite
{
    public sealed class SqliteMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public SqliteMessageQueue(ILogger<SqliteMessageQueue<TMessage>> logger, IOptions<SqliteMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _options = opts;

            _idleDelay = opts.IdleDelay ?? TimeSpan.FromMilliseconds(100);
            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>();
            _maxQueueSize = opts.MaxQueueSize;

            MaxWriteCount = opts.MaxWriteCount ?? 1;
            if (MaxWriteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opts.MaxWriteCount));
            }

            MaxReadCount = opts.MaxReadCount ?? 1;
            if (MaxReadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opts.MaxReadCount));
            }

            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
            {
                throw new ArgumentException($"{nameof(opts.ConnectionString)} is required", nameof(options));
            }

            using var dbContext = GetDatabaseContext();
            (_currentQueueSize, _sequenceNumber) = GetCurrentStats(dbContext);

            Name = opts.Name ?? nameof(SqliteMessageQueue<TMessage>);

            _logger.LogTrace($"{Name} initialized with {_currentQueueSize} stored messages");
        }

        private readonly SqliteMessageQueueOptions<TMessage> _options;

        private bool _disposed = false;
        private readonly SemaphoreSlim _readerSync = new(1, 1);

        private readonly ILogger _logger;
        private readonly TimeSpan _idleDelay;
        private readonly IMessageFormatter<TMessage, string> _messageFormatter;
        private readonly int? _maxQueueSize;

        private int _currentQueueSize;
        private long _sequenceNumber;

        private readonly CancellationTokenSource _cancellationSource = new();

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

        public int MaxWriteCount { get; }
        public int MaxReadCount { get; }


        private static (int QueueSize, long SequenceNumber) GetCurrentStats(SqliteDatabaseContext dbContext)
        {
            var queueSize = dbContext.SqliteQueueMessages.Count();

            var sequenceNumber = dbContext.SqliteQueueMessages
                .Select(x => x.SequenceNumber)
                .Max()
                ?? 0L;

            return (queueSize, sequenceNumber);
        }


        private SqliteDatabaseContext GetDatabaseContext()
        {
            var dbContextOptsBuilder = new DbContextOptionsBuilder<SqliteDatabaseContext>();
            dbContextOptsBuilder.UseSqlite(_options.ConnectionString);

            var dbContext = new SqliteDatabaseContext(dbContextOptsBuilder.Options);

            if (_options.OnDbContextCreated is { } onDbContextCreated)
            {
                onDbContextCreated(dbContext);
            }

            return dbContext;
        }


        public async Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            await PostManyMessagesAsync([message], cancellationToken);
        }

        public async Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            await PostManyMessagesAsync([(message, attributes)], cancellationToken);
        }

        public async Task PostManyMessagesAsync(IEnumerable<TMessage> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var messagesWithAtts = messages.Select(message => (message, _emptyAttributes));
            await PostManyMessagesAsync(messagesWithAtts, cancellationToken);
        }

        public async Task PostManyMessagesAsync(IEnumerable<(TMessage message, MessageAttributes attributes)> messages, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (messages is null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (!messages.Any())
            {
                throw new ArgumentOutOfRangeException(nameof(messages));
            }

            if (messages.Count() > MaxWriteCount)
            {
                _logger.LogError($"{Name} {nameof(PostManyMessagesAsync)} message count exceeds max write count of {MaxWriteCount}");
                throw new InvalidOperationException($"Message count exceeds max write count of {MaxWriteCount}");
            }

            if (_maxQueueSize is { } maxQueueSize)
            {
                if (_currentQueueSize >= maxQueueSize)
                {
                    _logger.LogError($"{Name} {nameof(PostManyMessagesAsync)} exceeded maximum queue size of {{MaxQueueSize}}", maxQueueSize);
                    throw new InvalidOperationException($"{Name} {nameof(PostManyMessagesAsync)} exceeded maximum queue size of {maxQueueSize}");
                }
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token, cancellationToken);
            linkedCancellation.Token.ThrowIfCancellationRequested();

            var sqlMessages = new List<SqliteQueueMessage>();
            foreach (var (message, attributes) in messages)
            {
                if (attributes is null)
                {
                    throw new ArgumentNullException(nameof(attributes));
                }

                var sqlMessage =
                    new SqliteQueueMessage()
                    {
                        Id = Guid.NewGuid(),
                        Attributes = JsonConvert.SerializeObject(attributes),
                        SequenceNumber = Interlocked.Add(ref _sequenceNumber, 1),
                        Body = await _messageFormatter.FormatMessage(message).ConfigureAwait(false)
                    };
                sqlMessages.Add(sqlMessage);
            }

            var messageCount = sqlMessages.Count;
            var messageString = messageCount == 1 ? sqlMessages[0].Body : $"{messageCount} messages";
            _logger.LogTrace($"{Name} {nameof(PostManyMessagesAsync)} posting to store, Message: {{Message}}", messageString);

            using (var dbContext = GetDatabaseContext())
            {
                dbContext.SqliteQueueMessages.AddRange(sqlMessages);
                _ = await dbContext.SaveChangesAsync(linkedCancellation.Token).ConfigureAwait(false);
            }

            _ = Interlocked.Add(ref _currentQueueSize, sqlMessages.Count);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var reader = new SqliteMessageReader<TMessage>(_logger, this, options);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        internal async Task<(CompletionResult, TResult)> InternalReadMessageAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, int count, object? userData, CancellationToken cancellationToken)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (count > MaxReadCount)
            {
                _logger.LogError($"{Name} {nameof(InternalReadMessageAsync)} read count exceeds max read count of {MaxReadCount}");
                throw new InvalidOperationException($"Read count exceeds max read count of {MaxReadCount}");
            }

            var shouldWait = false;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_cancellationSource.Token, cancellationToken);

            while (true)
            {
                if (shouldWait)
                {
                    await Task.Delay(_idleDelay, linkedCancellation.Token).ConfigureAwait(false);
                    shouldWait = false;
                }

                await _readerSync.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
                try
                {
                    linkedCancellation.Token.ThrowIfCancellationRequested();

                    if (_currentQueueSize == 0)
                    {
                        shouldWait = true;
                        continue;
                    }

                    using var dbContext = GetDatabaseContext();

                    var dbMessages = await dbContext.SqliteQueueMessages
                        .OrderBy(x => x.SequenceNumber)
                        .Take(count)
                        .ToListAsync(linkedCancellation.Token);

                    if (!dbMessages.Any())
                    {
                        shouldWait = true;
                        continue;
                    }

                    var items = new List<(SqliteQueueMessage item, TMessage message, MessageAttributes atts)>();
                    foreach (var item in dbMessages)
                    {
                        if (item.Attributes is null)
                        {
                            _logger.LogError($"{Name} {nameof(InternalReadMessageAsync)} message {{Id}} has null attributes", item.Id);
                            throw new Exception($"{item.Attributes} is required");
                        }

                        var atts = JsonConvert.DeserializeObject<MessageAttributes>(item.Attributes);
                        if (atts is null)
                        {
                            _logger.LogError($"{Name} {nameof(InternalReadMessageAsync)} message {{Id}} has invalid attributes: {{Attributes}}", item.Id, item.Attributes);
                            throw new FormatException($"{nameof(item.Attributes)} is invalid");
                        }

                        if (item.Body is null)
                        {
                            _logger.LogError($"{Name} {nameof(InternalReadMessageAsync)} message {{Id}} has null body", item.Id);
                            throw new Exception($"{nameof(item.Body)} is required");
                        }

                        var message = await _messageFormatter.RevertMessage(item.Body).ConfigureAwait(false);
                        items.Add((item, message, atts));
                    }

                    var messageAtts = items.Select(i => (i.message, i.atts)).ToList();
                    var (completionResult, result) = await action(messageAtts, userData, linkedCancellation.Token).ConfigureAwait(false);
                    if (completionResult == CompletionResult.Complete)
                    {
                        var itemsToRemove = items.Select(i => i.item).ToList();

                        dbContext.RemoveRange(itemsToRemove);

                        try
                        {
                            _ = await dbContext.SaveChangesAsync(linkedCancellation.Token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                (_currentQueueSize, _sequenceNumber) = GetCurrentStats(dbContext);
                            }
                            catch (Exception inner)
                            {
                                throw new AggregateException(ex, inner);
                            }

                            throw;
                        }

                        _ = Interlocked.Add(ref _currentQueueSize, -itemsToRemove.Count);
                    }

                    return (completionResult, result);
                }
                finally
                {
                    _ = _readerSync.Release();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SqliteMessageQueue<TMessage>));
            }
        }

        private void InternalDispose()
        {
            _cancellationSource.Cancel();
            _disposed = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            InternalDispose();

            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            InternalDispose();

            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
