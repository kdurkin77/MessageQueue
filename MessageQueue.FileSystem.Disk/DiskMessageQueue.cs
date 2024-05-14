using KM.MessageQueue.Formatters.ObjectToJsonObject;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.FileSystem.Disk
{
    public sealed class DiskMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        private readonly TimeSpan _idleDelay;
        private readonly int? _maxQueueSize;
        private readonly int _messagePartitionSize;

        private readonly IMessageFormatter<TMessage, JObject> _messageFormatter;


        private readonly DirectoryInfo _messageStore;
        private long _sequenceNumber;
        private readonly Queue<(FileInfo File, DiskMessage Message)> _messageQueue;

        private static readonly MessageAttributes _emptyAttributes = new();
        private static readonly string _messageExtension = @"msg.json.gzip";

        public DiskMessageQueue(ILogger<DiskMessageQueue<TMessage>> logger, IOptions<DiskMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _idleDelay = opts.IdleDelay ?? TimeSpan.FromMilliseconds(100);
            _maxQueueSize = opts.MaxQueueSize;
            _messagePartitionSize = opts.MessagePartitionSize ?? 5_000;

            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonObjectFormatter<TMessage>();

            _messageStore = opts.MessageStore ?? throw new ArgumentException($"{nameof(opts.MessageStore)} is required", nameof(options));

            if (!_messageStore.Exists)
            {
                Directory.CreateDirectory(_messageStore.FullName);
            }

            _messageQueue =
                new Queue<(FileInfo, DiskMessage)>(
                    _messageStore
                        .GetFiles($"*.{_messageExtension}", SearchOption.AllDirectories)
                        .Select(file =>
                        {
                            var compressedBytes = File.ReadAllBytes(file.FullName);
                            var fileBytes = Decompress(compressedBytes);
                            var fileJson = Encoding.UTF8.GetString(fileBytes);
                            var diskMessage = JsonConvert.DeserializeObject<DiskMessage>(fileJson);
                            return diskMessage is null
                                ? throw new Exception($"Improperly formatted disk message queue file: {file.FullName}")
                                : (file, diskMessage);
                        })
                        .OrderBy(item => item.diskMessage.SequenceNumber)
                    );

            _sequenceNumber = _messageQueue.Any()
                ? _messageQueue.Select(item => item.Message.SequenceNumber).Max()
                : 0L;

            Name = opts.Name ?? nameof(DiskMessageQueue<TMessage>);

            _logger.LogTrace($"initialized with {_messageQueue.Count} stored messages");
        }


        public string Name { get; }

        public int MaxWriteCount { get; } = 1;
        public int MaxReadCount { get; } = 1;

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

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();

                if (_maxQueueSize is { } maxQueueSize)
                {
                    if (_messageQueue.Count >= maxQueueSize)
                    {
                        throw new InvalidOperationException($"{Name} exceeded maximum queue size of {maxQueueSize}");
                    }
                }

                var (message, attributes) = messages.Single();
                var formattedMessage = await _messageFormatter.FormatMessage(message).ConfigureAwait(false);

                var diskMessage =
                    new DiskMessage(
                        id: Guid.NewGuid(),
                        sequenceNumber: ++_sequenceNumber,
                        attributes: attributes,
                        body: formattedMessage
                        );

                var file = await PersistMessageAsync(diskMessage, cancellationToken).ConfigureAwait(false);
                _messageQueue.Enqueue((file, diskMessage));
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task<FileInfo> PersistMessageAsync(DiskMessage diskMessage, CancellationToken cancellationToken)
        {
            if (diskMessage is null)
            {
                throw new ArgumentNullException(nameof(diskMessage));
            }

            var partition = diskMessage.SequenceNumber / _messagePartitionSize;
            var targetPath = Path.Combine(_messageStore.FullName, $"{partition}");
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            var fileName = Path.Combine(targetPath, $"{diskMessage.Id:N}.{_messageExtension}");

            _logger.LogTrace($"persisting {diskMessage.Id:N} to {fileName}");

            var fileJson = JsonConvert.SerializeObject(diskMessage);
            var fileBytes = Encoding.UTF8.GetBytes(fileJson);
            var compressedBytes = Compress(fileBytes);

#if NETSTANDARD2_0
            File.WriteAllBytes(fileName, compressedBytes);
            await Task.CompletedTask;
#else
            await File.WriteAllBytesAsync(fileName, compressedBytes, cancellationToken).ConfigureAwait(false);
#endif

            return new FileInfo(fileName);
        }

        private static byte[] Compress(byte[] data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            using var compressed = new MemoryStream();
            using var gzip = new GZipStream(compressed, CompressionMode.Compress);
            gzip.Write(data, 0, data.Length);

            return compressed.ToArray();
        }

        private static byte[] Decompress(byte[] compressed)
        {
            if (compressed is null)
            {
                throw new ArgumentNullException(nameof(compressed));
            }

            using var input = new MemoryStream(compressed);
            using var decompressed = new MemoryStream();
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            gzip.CopyTo(decompressed);

            return decompressed.ToArray();
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var reader = new DiskMessageQueueReader<TMessage>(_logger, this, options);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        internal async Task<(CompletionResult, TResult)> InternalReadMessageAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, object? userData, CancellationToken cancellationToken)
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

                    var item = _messageQueue.Peek();
                    var message = await _messageFormatter.RevertMessage(item.Message.Body).ConfigureAwait(false);
                    var (completionResult, result) = await action([ (message, item.Message.Attributes)], userData, cancellationToken).ConfigureAwait(false);
                    if (completionResult == CompletionResult.Complete)
                    {
                        item.File.Delete();
                        _messageQueue.Dequeue();
                    }

                    return (completionResult, result);
                }
                finally
                {
                    _sync.Release();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(Name);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

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

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
