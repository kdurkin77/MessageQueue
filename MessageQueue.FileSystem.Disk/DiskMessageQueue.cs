using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        private readonly ILogger _logger;
        private readonly DiskMessageQueueOptions _options;
        private readonly IMessageFormatter<TMessage> _formatter;

        private static readonly MessageAttributes _emptyAttributes = new MessageAttributes();

        private readonly SemaphoreSlim _sync;
        private readonly DirectoryInfo _messageStore;
        private long _sequenceNumber;
        private readonly Queue<(FileInfo File, DiskMessage Message)> _messageQueue;
        private static readonly string _messageExtension = @"msg.json.gzip";

        public DiskMessageQueue(ILogger<DiskMessageQueue<TMessage>> logger, IOptions<DiskMessageQueueOptions> options, IMessageFormatter<TMessage> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            if (_options.MessageStore is null)
            {
                throw new ArgumentException($"{nameof(_options)}.{nameof(_options.MessageStore)} cannot be null");
            }

            _sync = new SemaphoreSlim(1, 1);

            _messageStore = _options.MessageStore ?? throw new ArgumentException($"{nameof(_options)}.{nameof(_options.MessageStore)} cannot be null");

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
                            if(diskMessage is null)
                            {
                                throw new Exception($"Improperly formatted disk message queue file: {file.FullName}");
                            }

                            return (file, diskMessage);
                        })
                        .OrderBy(item => item.diskMessage.SequenceNumber)
                    );

            _sequenceNumber = _messageQueue.Any()
                ? _messageQueue.Select(item => item.Message.SequenceNumber).Max()
                : 0L;

            _logger.LogTrace($"initialized with {_messageQueue.Count} stored messages");
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

            await _sync.WaitAsync().ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();

                if (_options.MaxQueueSize.HasValue)
                {
                    if (_messageQueue.Count >= _options.MaxQueueSize.Value)
                    {
                        throw new InvalidOperationException("Maximum queue size exceeded");
                    }
                }

                var messageBytes = _formatter.MessageToBytes(message);

                var diskMessage =
                    new DiskMessage(
                        id: Guid.NewGuid(),
                        sequenceNumber: ++_sequenceNumber,
                        attributes: attributes,
                        body: messageBytes
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

            var partitionSize = _options.MessagePartitionSize ?? 5_000;

            var partition = diskMessage.SequenceNumber / partitionSize;
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

            using (var gzip = new GZipStream(compressed, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }

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

            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                gzip.CopyTo(decompressed);
            }

            return decompressed.ToArray();
        }

        public Task<IMessageReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new DiskMessageQueueReader<TMessage>(this);
            return Task.FromResult<IMessageReader<TMessage>>(reader);
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
                var result = await action(_formatter, item.Message.Body, item.Message.Attributes, userData, cancellationToken).ConfigureAwait(false);
                if (result == CompletionResult.Complete)
                {
                    item.File.Delete();
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
                throw new ObjectDisposedException(nameof(DiskMessageQueue<TMessage>));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            // not needed for this queue?

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if !NETSTANDARD2_0

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif
    }
}
