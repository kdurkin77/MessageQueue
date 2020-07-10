using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageQueue.AzureTopic
{
    public sealed class AzureTopicMessageQueueOptions<TMessage>
    {
        public string Endpoint { get; set; }
        public string EntityPath { get; set; }
        public string SharedAccessKeyName { get; set; }
        public string SharedAccessKey { get; set; }
        public TransportType TransportType { get; set; }
    }

    public sealed class AzureTopic<TMessage> : IMessageQueue<TMessage>
    {
        private bool _Disposed = false;
        private readonly TopicClient _TopicClient;
        private readonly AzureTopicMessageQueueOptions<TMessage> _Options;
        private readonly IMessageFormatter<TMessage> _Formatter;

        private static readonly MessageAttributes _EmptyAttributes = new MessageAttributes();

        public AzureTopic(IOptions<AzureTopicMessageQueueOptions<TMessage>> options, IMessageFormatter<TMessage> formatter)
        {
            this._Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this._Formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));

            var builder = new ServiceBusConnectionStringBuilder()
            {
                Endpoint = this._Options.Endpoint,
                EntityPath = this._Options.EntityPath,
                SasKey = this._Options.SharedAccessKey,
                SasKeyName = this._Options.SharedAccessKeyName,
                TransportType = this._Options.TransportType
            };

            this._TopicClient = new TopicClient(builder);
        }

        public Task PostMessageAsync(TMessage message, CancellationToken cancellationToken) => this.PostMessageAsync(message, _EmptyAttributes, cancellationToken);

        public async Task PostMessageAsync(TMessage message, MessageAttributes attributes, CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (attributes is null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var formattedMessageBytes = this._Formatter.Format(message);
            await this._TopicClient.SendAsync(new Message()
            {
                Body = formattedMessageBytes,
                Label = attributes.Label
            });
        }

        public void Dispose()
        {
            this._Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void _Dispose(bool disposing)
        {
            if (this._Disposed)
            {
                return;
            }

            if (disposing)
            {
                this._TopicClient.CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }

            this._Disposed = true;
        }

        ~AzureTopic() => this._Dispose(false);

#if NETSTANDARD2_1

        // https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
        public async ValueTask DisposeAsync()
        {
            await _TopicClient.CloseAsync().ConfigureAwait(false);
            _Dispose(false);
            GC.SuppressFinalize(this);
        }

#endif

    }
}
