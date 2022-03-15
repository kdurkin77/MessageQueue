using KM.MessageQueue.Http;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Extensions
    {
        /// <summary>
        /// Adds a <see cref="HttpMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpMessageQueue<TMessage>(this IServiceCollection services, Action<HttpMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddHttpMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Adds a <see cref="HttpMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddHttpMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, HttpMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<HttpMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new HttpMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<HttpMessageQueue<TMessage>>>();
                    return new HttpMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
