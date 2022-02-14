using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt.Tcp
{
    public sealed class TcpMqttMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false; 
        private readonly ILogger _logger;
        internal readonly TcpMqttMessageQueueOptions _options;
        internal readonly IMessageFormatter<TMessage> _formatter;
        internal readonly IManagedMqttClient _managedMqttClient;

        private static readonly MessageAttributes _emptyAttributes = new MessageAttributes();

        public TcpMqttMessageQueue(ILogger<TcpMqttMessageQueue<TMessage>> logger, IOptions<TcpMqttMessageQueueOptions> options, IMessageFormatter<TMessage> formatter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            
            _managedMqttClient = new MqttFactory().CreateManagedMqttClient();

            var clientOpts = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(_options.AutoReconnectDelay ?? TimeSpan.FromSeconds(5.0))
                .WithMaxPendingMessages(_options.MaxPendingMessages ?? int.MaxValue)
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(_options.Url)
                    .WithCredentials(_options.Username, _options.Password)
                    .WithCleanSession(_options.WithCleanSession ?? true)
                    .WithCommunicationTimeout(_options.CommunicationTimeout ?? TimeSpan.FromSeconds(10.0))
                    .WithProtocolVersion(_options.ProtocolVersion ?? MqttProtocolVersion.V311)
                    .Build())
                .Build();

            _managedMqttClient.StartAsync(clientOpts).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TcpMqttMessageQueue<TMessage>));
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

            var messageBytes = _formatter.MessageToBytes(message);

            _logger.LogTrace($"posting to {_options.Url}/{attributes.Label}");

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(attributes.Label)
                .WithPayload(messageBytes)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _managedMqttClient.PublishAsync(mqttMessage);
        }

        public Task<IMessageReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new TcpMqttMessageQueueReader<TMessage>(this);
            return Task.FromResult<IMessageReader<TMessage>>(reader);
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
