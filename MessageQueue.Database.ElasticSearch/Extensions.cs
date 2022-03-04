using KM.MessageQueue.Database.ElasticSearch;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElasticDatabaseExtensions
    {
        /// <summary>
        /// Add an <see cref="ElasticSearchMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddElasticSearchMessageQueue<TMessage>(this IServiceCollection services, Action<ElasticSearchMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddElasticSearchMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add an <see cref="ElasticSearchMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddElasticSearchMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, ElasticSearchMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<ElasticSearchMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new ElasticSearchMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<ElasticSearchMessageQueue<TMessage>>>();
                    return new ElasticSearchMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
