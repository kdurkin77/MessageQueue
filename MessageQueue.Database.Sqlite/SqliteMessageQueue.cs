﻿using KM.MessageQueue.Formatters.ObjectToJsonString;
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
        private bool _disposed = false;
        private long? _sequenceNumber;

        private readonly ILogger _logger;
        private readonly Queue<SqliteQueueMessage> _messageQueue;
        private readonly SemaphoreSlim _sync;

        internal readonly SqliteMessageQueueOptions<TMessage> _options;
        private readonly IMessageFormatter<TMessage, string> _messageFormatter;
        private readonly SqliteDatabaseContext _dbContext;

        private static readonly MessageAttributes _emptyAttributes = new();

        public SqliteMessageQueue(ILogger<SqliteMessageQueue<TMessage>> logger, IOptions<SqliteMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _messageFormatter = _options.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>();

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new ArgumentException($"{nameof(_options)}.{nameof(_options.ConnectionString)} cannot be null or whitespace");
            }

            //! to appease netstandard2.0 compiler, it doesn't realize that IsNullOrWhiteSpace is null checking
            var dbContextOptsBuilder = new DbContextOptionsBuilder<SqliteDatabaseContext>();
            dbContextOptsBuilder.UseSqlite(_options.ConnectionString);
            _dbContext = new SqliteDatabaseContext(dbContextOptsBuilder.Options);

            _sync = new SemaphoreSlim(1, 1);

            _messageQueue = new Queue<SqliteQueueMessage>(
                _dbContext.SqliteQueueMessages
                .OrderBy(item => item.SequenceNumber)
                );

            _sequenceNumber = _messageQueue.Any() 
                ? _messageQueue.Select(item => item.SequenceNumber).Max() : 0L;

            _logger.LogTrace($"{nameof(SqliteMessageQueue<TMessage>)} initialized with {_messageQueue.Count} stored messages");
        }

        public Task PostMessageAsync(TMessage message, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return PostMessageAsync(message, _emptyAttributes, cancellationToken);
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

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_options.MaxQueueSize.HasValue)
                {
                    if (_messageQueue.Count >= _options.MaxQueueSize.Value)
                    {
                        throw new InvalidOperationException("Maximum queue size exceeded");
                    }
                }

                var messageBytes = await _messageFormatter.FormatMessage(message);

                var sqlMessage =
                    new SqliteQueueMessage()
                    {
                        Id = Guid.NewGuid(),
                        Attributes = JsonConvert.SerializeObject(attributes),
                        SequenceNumber = ++_sequenceNumber,
                        Body = messageBytes
                    };

                _logger.LogTrace($"Posting to {nameof(SqliteMessageQueue<TMessage>)}, Label: {{Label}}, Message: {{Message}}", attributes.Label, sqlMessage);

                _dbContext.SqliteQueueMessages.Add(sqlMessage);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                _messageQueue.Enqueue(sqlMessage);
            }
            finally
            {
                _sync.Release();
            }
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new SqliteMessageReader<TMessage>(this);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        internal async Task<bool> TryReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, object? userData, CancellationToken cancellationToken)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!_messageQueue.Any())
                {
                    return false;
                }

                var item = _messageQueue.Peek();

                if (item.Attributes is null)
                {
                    throw new Exception("Attributes cannot be null");
                }
                var atts = JsonConvert.DeserializeObject<MessageAttributes>(item.Attributes) ?? throw new Exception("Attributes formatted incorrectly");
                
                if (item.Body is null)
                {
                    throw new Exception("Body cannot be null");
                }
                var message = await _messageFormatter.RevertMessage(item.Body);
                var result = await action(message, atts, userData, cancellationToken).ConfigureAwait(false);
                if (result == CompletionResult.Complete)
                {
                    _dbContext.Remove(item);
                    await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    _messageQueue.Dequeue();
                }

                return true;
            }
            finally
            {
                _sync.Release();
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

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif
    }
}
