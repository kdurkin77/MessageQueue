using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt
{
    internal sealed class MqttMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        public MqttMessageQueueReader(ILogger logger, MqttMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var factory = new MqttFactory();
            _mqttReaderClient = factory.CreateManagedMqttClient(_queue._mqttClient.InternalClient);

            _subscriptionName = options.SubscriptionName;
            _userData = options.UserData;

            _channel = Channel.CreateBounded<(TMessage, MessageAttributes, TaskCompletionSource<CancellationToken>, TaskCompletionSource<CompletionResult>)>(capacity: 1);

            Name = options.Name ?? nameof(MqttMessageQueueReader<TMessage>);

            MqttMessageQueue<TMessage>.EnsureConnectedAsync(_sync, _mqttReaderClient, _queue._mqttClientOptions, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            EnsureSubscribedAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        }


        private bool _disposed = false;
        private bool _subscribed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);
        private readonly SemaphoreSlim _syncWriter = new(1, 1);
        private readonly CancellationTokenSource _cancellationSource = new();

        private readonly ILogger _logger;
        private readonly MqttMessageQueue<TMessage> _queue;
        internal readonly IManagedMqttClient _mqttReaderClient;
        private readonly string? _subscriptionName;
        private readonly object? _userData;

        private readonly Channel<(TMessage, MessageAttributes, TaskCompletionSource<CancellationToken>, TaskCompletionSource<CompletionResult>)> _channel;


        public string Name { get; }

        public async Task<CompletionResult> ReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var (completionResult, _) = await ReadMessageAsync(Wrapper, cancellationToken).ConfigureAwait(false);

            return completionResult;


            async Task<(CompletionResult CompletionResult, int)> Wrapper(TMessage message, MessageAttributes attributes, object? userData, CancellationToken cancellationToken)
            {
                var completionResult = await action(message, attributes, userData, cancellationToken).ConfigureAwait(false);
                return (completionResult, 0);
            }
        }

        private async Task EnsureSubscribedAsync(CancellationToken cancellationToken)
        {
            if (_subscribed)
            {
                return;
            }

            await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_subscribed)
                {
                    return;
                }

                // do something with this result?
                //var result = await _mqttReaderClient.SubscribeAsync(_subscriptionName, MqttQualityOfServiceLevel.ExactlyOnce, cancellationToken).ConfigureAwait(false);
                var filters = new[]
                {
                    new MqttTopicFilterBuilder().WithTopic(_subscriptionName).Build()
                };

                await _mqttReaderClient.SubscribeAsync(filters).ConfigureAwait(false);

                _mqttReaderClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

                _subscribed = true;
            }
            finally
            {
                _ = _sync.Release();
            }
        }

        public async Task<(CompletionResult CompletionResult, TResult Result)> ReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            // specific reader connection
            await MqttMessageQueue<TMessage>.EnsureConnectedAsync(_sync, _mqttReaderClient, _queue._mqttClientOptions, cancellationToken).ConfigureAwait(false);

            await EnsureSubscribedAsync(cancellationToken).ConfigureAwait(false);

            var (message, attributes, cancellationCompletionSource, resultCompletionSource) = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            using var cancellationSource = new CancellationTokenSource();
            cancellationCompletionSource.SetResult(cancellationSource.Token);

            try
            {
                var (completionResult, result) = await action(message, attributes, _userData, cancellationToken).ConfigureAwait(false);

                resultCompletionSource.SetResult(completionResult);

                return (completionResult, result);
            }
            catch
            {
                cancellationSource.Cancel();
                throw;
            }
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            arg.AutoAcknowledge = false;

            await _syncWriter.WaitAsync(_cancellationSource.Token).ConfigureAwait(false);
            try
            {
                var attributes = new MessageAttributes()
                {
                    ContentType = arg.ApplicationMessage.ContentType,
                    Label = arg.ApplicationMessage.Topic,
                    UserProperties = arg.ApplicationMessage.UserProperties?.ToDictionary(prop => prop.Name, prop => (object)prop.Value)
                };

                var message = await _queue._messageFormatter.RevertMessage(arg.ApplicationMessage.PayloadSegment.ToArray()).ConfigureAwait(false);

                var cancellationCompletionSource = new TaskCompletionSource<CancellationToken>();
                var resultCompletionSource = new TaskCompletionSource<CompletionResult>();

                await _channel.Writer.WriteAsync((message, attributes, cancellationCompletionSource, resultCompletionSource), _cancellationSource.Token).ConfigureAwait(false);

                var cancellationToken = await cancellationCompletionSource.Task.ConfigureAwait(false);

                var completionResult = await Task.Run(async () => await resultCompletionSource.Task.ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                if (completionResult == CompletionResult.Complete)
                {
                    await arg.AcknowledgeAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                _ = _syncWriter.Release();
            }
        }

        public async Task<(bool, CompletionResult)> TryReadMessageAsync(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var completionResult = await ReadMessageAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult);
            }
            catch (OperationCanceledException)
            {
                return (false, default);
            }
        }

        public async Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var (completionResult, result) = await ReadMessageAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult, result);
            }
            catch (OperationCanceledException)
            {
                return (false, default, default!);
            }
        }


        private async Task ShutdownAsync()
        {
            await _syncWriter.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                _cancellationSource.Cancel();
                _mqttReaderClient.Dispose();
                //_channel.Writer.Complete();
            }
            finally
            {
                _ = _syncWriter.Release();
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

            ShutdownAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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

            await ShutdownAsync().ConfigureAwait(false);

            _disposed = true;
            GC.SuppressFinalize(this);

            await Task.CompletedTask;
        }

#endif
    }
}
