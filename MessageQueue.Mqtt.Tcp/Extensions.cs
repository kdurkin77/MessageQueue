using KM.MessageQueue;
using KM.MessageQueue.Formatters.ToJsonString;
using KM.MessageQueue.Formatters.StringToBytes;
using KM.MessageQueue.Mqtt.Tcp;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TcpMqttExtensions
    {
        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<TcpMqttMessageQueueOptions> configureOptions)
        {
            return services.AddTcpMqttMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, TcpMqttMessageQueueOptions> configureOptions)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return services
                .AddMessageQueue<TcpMqttMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new TcpMqttMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<TcpMqttMessageQueue<TMessage>>>();
                    var formatter = new JsonStringFormatter<TMessage>().Compose(new StringToBytesFormatter());
                    return new TcpMqttMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }

        public static IServiceCollection AddTcpMqttMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<TcpMqttMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, byte[]>
        {
            return services.AddTcpMqttMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddTcpMqttMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<IServiceProvider, TcpMqttMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, byte[]>
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions is null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return services
                .AddMessageQueue<TcpMqttMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new TcpMqttMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<TcpMqttMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<TFormatter>();
                    return new TcpMqttMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
