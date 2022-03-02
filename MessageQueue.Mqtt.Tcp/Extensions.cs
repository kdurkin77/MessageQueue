using KM.MessageQueue;
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
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage, byte[]>>();
                    return new TcpMqttMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
