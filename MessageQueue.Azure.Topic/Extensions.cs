using KM.MessageQueue.Azure.Topic;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureTopicMessageQueueExtensions
    {
        /// <summary>
        /// Add an <see cref="AzureTopicMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<AzureTopicMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddAzureTopicMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add an <see cref="AzureTopicMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, AzureTopicMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<AzureTopicMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new AzureTopicMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<AzureTopicMessageQueue<TMessage>>>();
                    return new AzureTopicMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
