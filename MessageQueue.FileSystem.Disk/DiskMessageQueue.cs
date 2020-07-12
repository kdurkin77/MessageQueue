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
        private readonly DiskMessageQueueOptions<TMessage> _options;
        private readonly IMessageFormatter<TMessage> _formatter;

        private static readonly MessageAttributes _emptyAttributes = new MessageAttributes();

        private readonly SemaphoreSlim _sync;
        private readonly DirectoryInfo _messageStore;
        private long _sequenceNumber;
        private readonly Queue<(FileInfo File, DiskMessage Message)> _messageQueue;
        private static readonly string _messageExtension = @"msg.json.gzip";

        public DiskMessageQueue(IOptions<DiskMessageQueueOptions<TMessage>> options, IMessageFormatter<TMessage> formatter)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            if (_options.MessageStore is null)
            {
                throw new ArgumentNullException(nameof(_options.MessageStore));
            }

            _sync = new SemaphoreSlim(1, 1);

            _messageStore = _options.MessageStore ?? throw new ArgumentNullException(nameof(_options.MessageStore));

            if (!_messageStore.Exists)
            {
                Directory.CreateDirectory(_messageStore.FullName);
            }

            _messageQueue =
                new Queue<(FileInfo, DiskMessage)>(
                    _messageStore
                        .GetFiles($"*.{_messageExtension}")
                        .Select(file =>
                        {
                            var compressedBytes = File.ReadAllBytes(file.FullName);
                            var fileBytes = Decompress(compressedBytes);
                            var fileJson = Encoding.UTF8.GetString(fileBytes);
                            var diskMessage = JsonConvert.DeserializeObject<DiskMessage>(fileJson);
                            return (file, diskMessage);
                        })
                        .OrderBy(item => item.diskMessage.SequenceNumber)
                    );

            _sequenceNumber = _messageQueue.Any()
                ? _messageQueue.Select(item => item.Message.SequenceNumber).Max()
                : 0L;
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

                var messageBytes = _formatter.Format(message);

                var diskMessage = new DiskMessage()
                {
                    Id = Guid.NewGuid(),
                    SequenceNumber = ++_sequenceNumber,
                    Attributes = attributes,
                    Body = messageBytes
                };

                var file = await PersistMessageAsync(diskMessage, cancellationToken);

                _messageQueue.Enqueue((file, diskMessage));
            }
            finally
            {
                _sync.Release();
            }
        }

        private async Task<FileInfo> PersistMessageAsync(DiskMessage diskMessage, CancellationToken cancellationToken)
        {
            var fileName = Path.Combine(_messageStore.FullName, $"{diskMessage.Id:N}.{_messageExtension}");
            var fileJson = JsonConvert.SerializeObject(diskMessage);
            var fileBytes = Encoding.UTF8.GetBytes(fileJson);
            var compressedBytes = Compress(fileBytes);

#if NETSTANDARD2_0
            File.WriteAllBytes(fileName, compressedBytes);
            await Task.CompletedTask;
#else
            await File.WriteAllBytesAsync(fileName, compressedBytes, cancellationToken);
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
            throw new NotImplementedException();
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // not needed for this queue?
            }

            _disposed = true;
        }

        ~DiskMessageQueue() => Dispose(false);

#if !NETSTANDARD2_0

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(false);
            GC.SuppressFinalize(this);

            // compiler appeasement
            await Task.CompletedTask;
        }

#endif

    }
}
