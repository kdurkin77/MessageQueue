using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt.Tcp
{
    public sealed class TcpMqttMessageQueue<TMessage> : IMessageQueue<TMessage>
    {
        private bool _disposed = false; 
        private readonly ILogger _logger;
        internal readonly TcpMqttMessageQueueOptions<TMessage> _options;
        internal readonly IManagedMqttClient _managedMqttClient;

        private static readonly MessageAttributes _emptyAttributes = new();

        public TcpMqttMessageQueue(ILogger<TcpMqttMessageQueue<TMessage>> logger, IOptions<TcpMqttMessageQueueOptions<TMessage>> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _managedMqttClient = new MqttFactory().CreateManagedMqttClient();
            var clientOpts = _options.ManagedMqttClientOptions ?? throw new ArgumentException($"{nameof(options)}.{nameof(_options.ManagedMqttClientOptions)} cannot be null");
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

            var messageBytes = _options.MessageFormatter.FormatMessage(message);

            _logger.LogTrace($"posting to {nameof(TcpMqttMessageQueue<TMessage>)} - {attributes.Label}");

            //To Do: find a way to make these optional
            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(attributes.Label)
                .WithPayload(messageBytes)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _managedMqttClient.PublishAsync(mqttMessage).ConfigureAwait(false);
        }

        public Task<IMessageQueueReader<TMessage>> GetReaderAsync(CancellationToken cancellationToken)
        {
            var reader = new TcpMqttMessageQueueReader<TMessage>(this);
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
