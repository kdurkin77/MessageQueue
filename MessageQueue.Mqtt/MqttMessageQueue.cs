using KM.MessageQueue.Formatters.ObjectToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt
{
    public sealed class MqttMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false; 
        private readonly ILogger _logger;
        internal readonly MqttMessageQueueOptions<TMessage> _options;
        internal readonly IMessageFormatter<TMessage, byte[]> _messageFormatter;
        internal readonly Func<byte[], MessageAttributes, MqttApplicationMessage> _messageBuilder;
        internal readonly IManagedMqttClient _managedMqttClient;

        private static readonly MessageAttributes _emptyAttributes = new();

        public MqttMessageQueue(ILogger<MqttMessageQueue<TMessage>> logger, IOptions<MqttMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _messageFormatter = _options.MessageFormatter ?? new ObjectToJsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
            _messageBuilder = _options.MessageBuilder ??
                ((payload, attributes) =>
                new MqttApplicationMessageBuilder()
                    .WithTopic(attributes.Label)
                    .WithPayload(payload)
                    .WithExactlyOnceQoS()
                    .WithRetainFlag()
                    .Build());
            _managedMqttClient = new MqttFactory().CreateManagedMqttClient();
            var clientOpts = _options.ManagedMqttClientOptions ?? throw new ArgumentException($"{nameof(options)}.{nameof(_options.ManagedMqttClientOptions)} cannot be null");
            _managedMqttClient.StartAsync(clientOpts).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MqttMessageQueue<TMessage>));
            }
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

            var messageBytes = _messageFormatter.FormatMessage(message);

            _logger.LogTrace($"posting to {nameof(MqttMessageQueue<TMessage>)} - {attributes.Label}");

            var mqttMessage = _messageBuilder(messageBytes, attributes);
            await _managedMqttClient.PublishAsync(mqttMessage).ConfigureAwait(false);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new MqttMessageQueueReader<TMessage>(this);
            return Task.FromResult<IMessageQueueReader<TMessage>>(reader);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _managedMqttClient.StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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

            await  _managedMqttClient.StopAsync().ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#endif
    }
}
