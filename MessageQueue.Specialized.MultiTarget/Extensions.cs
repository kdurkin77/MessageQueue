using System;
using Microsoft.Extensions.Logging;
using KM.MessageQueue.Specialized.MultiTarget;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MultiTargetMessageQueueExtensions
    {
        /// <summary>
        /// Adds a <see cref="MultiTargetMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddMultiTargetMessageQueue<TMessage>(this IServiceCollection services, Action<MultiTargetMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddMultiTargetMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Adds a <see cref="MultiTargetMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddMultiTargetMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, MultiTargetMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<MultiTargetMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new MultiTargetMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<MultiTargetMessageQueue<TMessage>>>();
                    return new MultiTargetMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
