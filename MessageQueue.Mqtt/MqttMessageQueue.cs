using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt
{
    public sealed class MqttMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        public MqttMessageQueue(ILogger<MqttMessageQueue<TMessage>> logger, IOptions<MqttMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _messageFormatter = opts.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());

            MaxReadCount = opts.MaxReadCount ?? 1;
            if (MaxReadCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opts.MaxReadCount));
            }

            _messageBuilder = opts.MessageBuilder
                ?? ((payload, attributes) => new MqttApplicationMessageBuilder()
                        .WithTopic(attributes.Label)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(attributes.QualityOfService())
                        .WithRetainFlag(attributes.RetainMessage())
                        .Build());

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttCreateClientOptionsBuilder = opts.CreateClientOptionsBuilder ?? throw new ArgumentException($"{nameof(opts.CreateClientOptionsBuilder)} is required", nameof(options));
            _mqttClientOptions = _mqttCreateClientOptionsBuilder().Build();

            EnsureConnectedAsync(_sync, _mqttClient, _mqttClientOptions, _logger, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            Name = opts.Name ?? nameof(MqttMessageQueue<TMessage>);
        }


        private bool _disposed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);

        private readonly ILogger _logger;
        internal readonly IMessageFormatter<TMessage, byte[]> _messageFormatter;
        private readonly Func<byte[], MessageAttributes, MqttApplicationMessage> _messageBuilder;
        private readonly IMqttClient _mqttClient;
        internal readonly Func<MqttClientOptionsBuilder> _mqttCreateClientOptionsBuilder;
        private readonly MqttClientOptions _mqttClientOptions;

        private static readonly MessageAttributes _emptyAttributes = new();


        public string Name { get; }

        public int MaxWriteCount { get; } = 1;
        public int MaxReadCount { get; }

        internal static async Task<bool> EnsureConnectedAsync(SemaphoreSlim sync, IMqttClient mqttClient, MqttClientOptions mqttClientOptions, ILogger logger, CancellationToken cancellationToken)
        {
            if (mqttClient.IsConnected)
            {
                return false;
            }

            await sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (mqttClient.IsConnected)
                {
                    return false;
                }

                var result = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken).ConfigureAwait(false);
                if (result.ResultCode != MqttClientConnectResultCode.Success)
                {
                    logger.LogError($"{{clientId}} failed to connect: {{resultCode}}", mqttClient.Options.ClientId, result.ResultCode);
                }

                return true;
            }
            finally
            {
                _ = sync.Release();
            }
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

            await EnsureConnectedAsync(_sync, _mqttClient, _mqttClientOptions, _logger, cancellationToken).ConfigureAwait(false);

            var (message, attributes) = messages.Single();
            var messageBytes = await _messageFormatter.FormatMessage(message).ConfigureAwait(false);
            var mqttMessage = _messageBuilder(messageBytes, attributes);

            _logger.LogTrace($"{{Name}} {nameof(PostMessageAsync)} posting to Label: {{Label}}", Name, attributes.Label);
            await _mqttClient.PublishAsync(mqttMessage, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IMessageQueueReader<TMessage>> GetReaderAsync(MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var reader = await MqttMessageQueueReader<TMessage>.CreateAsync(_logger, this, options, cancellationToken).ConfigureAwait(false);
            return reader;
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

            _mqttClient.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _mqttClient.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
