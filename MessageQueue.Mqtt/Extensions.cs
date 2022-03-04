using KM.MessageQueue.Mqtt;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MqttExtensions
    {
        /// <summary>
        /// Add a <see cref="MqttMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddMqttMessageQueue<TMessage>(this IServiceCollection services, Action<MqttMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddMqttMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add a <see cref="MqttMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddMqttMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, MqttMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<MqttMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new MqttMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<MqttMessageQueue<TMessage>>>();
                    return new MqttMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
