using KM.MessageQueue;
using KM.MessageQueue.Mqtt.Tcp;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TcpMqttExtensions
    {
        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<TcpMqttOptions> configureOptions)
        {
            return services.AddTcpMqttMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, TcpMqttOptions> configureOptions)
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
                .AddMessageQueue<TcpMqtt<TMessage>, TMessage>(services =>
                {
                    var options = new TcpMqttOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<TcpMqtt<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new TcpMqtt<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
