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

            _idleDelay = opts.IdleDelay ?? TimeSpan.FromMilliseconds(100);
            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>();
            _maxQueueSize = opts.MaxQueueSize;

            if (string.IsNullOrWhiteSpace(opts.ConnectionString))
            {
                throw new ArgumentException($"{nameof(opts.ConnectionString)} is required", nameof(options));
            }

            var dbContextOptsBuilder = new DbContextOptionsBuilder<SqliteDatabaseContext>();
            dbContextOptsBuilder.UseSqlite(opts.ConnectionString);

            _dbContext = new SqliteDatabaseContext(dbContextOptsBuilder.Options);

            if (opts.OnDbContextCreated is { } onDbContextCreated)
            {
                onDbContextCreated(_dbContext);
            }

            _messageQueue = new LinkedList<SqliteQueueMessage>(
                _dbContext.SqliteQueueMessages
                    .OrderBy(item => item.SequenceNumber)
                );

            _sequenceNumber = _messageQueue.Any()
                ? _messageQueue.Select(item => item.SequenceNumber).Max()
                : 0L;

            Name = opts.Name ?? nameof(SqliteMessageQueue<TMessage>);

            _logger.LogTrace($"{Name} initialized with {_messageQueue.Count} stored messages");
        }


        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        private readonly TimeSpan _idleDelay;
        private readonly IMessageFormatter<TMessage, string> _messageFormatter;
        private readonly int? _maxQueueSize;
        private readonly LinkedList<SqliteQueueMessage> _messageQueue;
        private readonly SqliteDatabaseContext _dbContext;
        private long? _sequenceNumber;

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

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

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_maxQueueSize is { } maxQueueSize)
                {
                    if (_messageQueue.Count >= maxQueueSize)
                    {
                        _logger.LogError($"{Name} {nameof(PostMessageAsync)} exceeded maximum queue size of {{MaxQueueSize}}", maxQueueSize);
                        throw new InvalidOperationException($"{Name} {nameof(PostMessageAsync)} exceeded maximum queue size of {maxQueueSize}");
                    }
                }

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
                            SequenceNumber = ++_sequenceNumber,
                            Body = await _messageFormatter.FormatMessage(message).ConfigureAwait(false)
                        };
                    sqlMessages.Add(sqlMessage);
                }

                var messageCount = sqlMessages.Count();
                var messageString = messageCount == 1 ? sqlMessages[0].Body : $"{messageCount} messages";
                _logger.LogTrace($"{Name} {nameof(PostMessageAsync)} posting to store, Message: {{Message}}", messageString);

                _dbContext.SqliteQueueMessages.AddRange(sqlMessages);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                foreach (var message in sqlMessages)
                {
                    _messageQueue.AddLast(message);
                }
            }
            finally
            {
                _ = _sync.Release();
            }
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

            while (true)
            {
                await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (!_messageQueue.Any())
                    {
                        await Task.Delay(_idleDelay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    var items =
                        await Task.WhenAll(
                        _messageQueue
                        .Take(count)
                        .Select(async item =>
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
                            return (item, message, atts);
                        }).ToList()).ConfigureAwait(false);

                    var messageAtts = items.Select(i => (i.message, i.atts)).ToList();
                    var (completionResult, result) = await action(messageAtts, userData, cancellationToken).ConfigureAwait(false);
                    if (completionResult == CompletionResult.Complete)
                    {
                        var itemsToRemove = items.Select(i => i.item).ToList();
                        _dbContext.RemoveRange(itemsToRemove);
                        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                        for (var i = 0; i < itemsToRemove.Count(); i++)
                        {
                            _messageQueue.RemoveFirst();
                        }
                    }

                    return (completionResult, result);
                }
                finally
                {
                    _ = _sync.Release();
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _dbContext.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _dbContext.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
