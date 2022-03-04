using KM.MessageQueue.Mqtt.Tcp;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class TcpMqttExtensions
    {
        /// <summary>
        /// Add a <see cref="TcpMqttMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<TcpMqttMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddTcpMqttMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add a <see cref="TcpMqttMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddTcpMqttMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, TcpMqttMessageQueueOptions<TMessage>> configureOptions)
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
                    var options = new TcpMqttMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<TcpMqttMessageQueue<TMessage>>>();
                    return new TcpMqttMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
