﻿using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KM.MessageQueue.Mqtt
{
    internal sealed class MqttMessageQueueReader<TMessage> : IMessageQueueReader<TMessage>
    {
        private MqttMessageQueueReader(ILogger logger, MqttMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var factory = new MqttFactory();
            _mqttReaderClient = factory.CreateMqttClient();
            _mqttReaderClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            _subscriptionName = options.SubscriptionName;
            _userData = options.UserData;
            _readCount = options.ReadCount ?? 1;
            if (_readCount <= 0 || _readCount > queue.MaxReadCount)
            {
                throw new ArgumentOutOfRangeException(nameof(options.ReadCount), $"{options.ReadCount} must be greater than 0 and cannot be greater than {queue.MaxReadCount}");
            }

            Name = options.Name ?? nameof(MqttMessageQueueReader<TMessage>);

            _clientOptions = _queue._mqttCreateClientOptionsBuilder().Build();
        }


        private bool _disposed = false;
        private bool _subscribed = false;
        private readonly SemaphoreSlim _sync = new(1, 1);
        private readonly CancellationTokenSource _cancellationSource = new();
        private readonly LinkedList<(TMessage Message, MessageAttributes Attributes)> _messages = new();

        private readonly ILogger _logger;
        private readonly MqttMessageQueue<TMessage> _queue;
        private readonly MqttClientOptions _clientOptions;
        private readonly IMqttClient _mqttReaderClient;
        private readonly string? _subscriptionName;
        private readonly object? _userData;
        private readonly int _readCount;


        public string Name { get; }


        internal static async Task<MqttMessageQueueReader<TMessage>> CreateAsync(ILogger logger, MqttMessageQueue<TMessage> queue, MessageQueueReaderOptions<TMessage> options, CancellationToken cancellationToken)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (queue is null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var reader = new MqttMessageQueueReader<TMessage>(logger, queue, options);

            await reader.EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
            await reader.EnsureSubscribedAsync(cancellationToken).ConfigureAwait(false);

            return reader;
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            var reconnected = await MqttMessageQueue<TMessage>.EnsureConnectedAsync(_sync, _mqttReaderClient, _clientOptions, _logger, cancellationToken).ConfigureAwait(false);
            if (reconnected)
            {
                _subscribed = false;
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
                var filter = new MqttTopicFilterBuilder().WithTopic(_subscriptionName).Build();
                _ = await _mqttReaderClient.SubscribeAsync(filter, cancellationToken).ConfigureAwait(false);

                //var filters = new[]
                //{
                //    new MqttTopicFilterBuilder().WithTopic(_subscriptionName).Build()
                //};

                //await _mqttReaderClient.SubscribeAsync(filters).ConfigureAwait(false);

                _subscribed = true;
            }
            finally
            {
                _ = _sync.Release();
            }
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            await _sync.WaitAsync(_cancellationSource.Token).ConfigureAwait(false);
            try
            {
                var attributes = new MessageAttributes()
                {
                    ContentType = arg.ApplicationMessage.ContentType,
                    Label = arg.ApplicationMessage.Topic,
                    UserProperties = arg.ApplicationMessage.UserProperties?.ToDictionary(prop => prop.Name, prop => (object)prop.Value)
                };

                var message = await _queue._messageFormatter.RevertMessage([.. arg.ApplicationMessage.PayloadSegment]).ConfigureAwait(false);

                _messages.AddLast((message, attributes));
            }
            finally
            {
                _ = _sync.Release();
            }
        }

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

        public async Task<(CompletionResult CompletionResult, TResult Result)> ReadMessageAsync<TResult>(Func<TMessage, MessageAttributes, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            while (true)
            {
                // specific reader connection
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                await EnsureSubscribedAsync(cancellationToken).ConfigureAwait(false);

                await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_messages.Any())
                    {
                        var (message, attributes) = _messages.First();
                        var (completionResult, result) = await action(message, attributes, _userData, cancellationToken).ConfigureAwait(false);
                        if (completionResult == CompletionResult.Complete)
                        {
                            _messages.RemoveFirst();
                        }

                        return (completionResult, result);
                    }
                }
                finally
                {
                    _ = _sync.Release();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<CompletionResult> ReadManyMessagesAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var (completionResult, _) = await ReadManyMessagesAsync(Wrapper, cancellationToken).ConfigureAwait(false);

            return completionResult;


            async Task<(CompletionResult CompletionResult, int)> Wrapper(IEnumerable<(TMessage, MessageAttributes)> messages, object? userData, CancellationToken cancellationToken)
            {
                var completionResult = await action(messages, userData, cancellationToken).ConfigureAwait(false);
                return (completionResult, 0);
            }
        }

        public async Task<(CompletionResult CompletionResult, TResult Result)> ReadManyMessagesAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            while (true)
            {
                // specific reader connection
                await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
                await EnsureSubscribedAsync(cancellationToken).ConfigureAwait(false);

                await _sync.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (_messages.Any())
                    {
                        var items = _messages.Take(_readCount).ToList();
                        var (completionResult, result) = await action(items, _userData, cancellationToken).ConfigureAwait(false);
                        if (completionResult == CompletionResult.Complete)
                        {
                            for (var i = 0; i < items.Count; i++)
                            {
                                _messages.RemoveFirst();
                            }
                        }

                        return (completionResult, result);
                    }
                }
                finally
                {
                    _ = _sync.Release();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
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

        public async Task<(bool, CompletionResult)> TryReadManyMessagesAsync(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<CompletionResult>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var completionResult = await ReadManyMessagesAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult);
            }
            catch (OperationCanceledException)
            {
                return (false, default);
            }
        }

        public async Task<(bool Success, CompletionResult CompletionResult, TResult Result)> TryReadManyMessagesAsync<TResult>(Func<IEnumerable<(TMessage, MessageAttributes)>, object?, CancellationToken, Task<(CompletionResult, TResult)>> action, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            try
            {
                var (completionResult, result) = await ReadManyMessagesAsync(action, cancellationToken).ConfigureAwait(false);
                return (true, completionResult, result);
            }
            catch (OperationCanceledException)
            {
                return (false, default, default!);
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

            _cancellationSource.Cancel();
            _mqttReaderClient.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

#if NETSTANDARD2_1_OR_GREATER || NET

        public async ValueTask DisposeAsync()
        {
            Dispose();
            await Task.CompletedTask;
        }

#endif

    }
}
