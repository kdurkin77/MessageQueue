using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Sqlite
{
    public sealed class SqliteMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private long _sequenceNumber;

        private readonly ILogger _logger;
        private readonly Queue<SqliteQueueMessage> _messageQueue;
        private readonly SemaphoreSlim _sync;

        internal readonly SqliteMessageQueueOptions _options;
        internal readonly IMessageFormatter<TMessage> _formatter;
        internal readonly SqliteDatabaseContext _dbContext;

        private static readonly MessageAttributes _emptyAttributes = new MessageAttributes();

        public SqliteMessageQueue(ILogger<SqliteMessageQueue<TMessage>> logger, IOptions<SqliteMessageQueueOptions> options, IMessageFormatter<TMessage> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            {
                throw new ArgumentException($"{nameof(_options)}.{nameof(_options.ConnectionString)} cannot be null or whitespace");
            }

            //! to appease netstandard2.0 compiler, it doesn't realize that IsNullOrWhiteSpace is null checking
            _dbContext = new SqliteDatabaseContext(_options.ConnectionString!);

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

                var messageBytes = _formatter.MessageToBytes(message);

                var sqlMessage =
                    new SqliteQueueMessage()
                    {
                        Id = Guid.NewGuid(),
                        Attributes = JsonConvert.SerializeObject(attributes),
                        SequenceNumber = ++_sequenceNumber,
                        Body = messageBytes
                    };

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

        internal async Task<bool> TryReadMessageAsync(Func<IMessageFormatter<TMessage>, byte[], MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, object? userData, CancellationToken cancellationToken)
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
                var atts = JsonConvert.DeserializeObject<MessageAttributes>(item.Attributes) ?? throw new Exception("Attributes formatted incorrectly");
                var result = await action(_formatter, item.Body, atts, userData, cancellationToken).ConfigureAwait(false);
                if (result == CompletionResult.Complete)
                {
                    _dbContext.Remove(item);
                    await _dbContext.SaveChangesAsync(cancellationToken);
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
