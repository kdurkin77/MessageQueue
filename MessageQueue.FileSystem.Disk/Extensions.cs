using KM.MessageQueue.FileSystem.Disk;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiskMessageQueueExtensions
    {
        /// <summary>
        /// Add a <see cref="DiskMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddDiskMessageQueue<TMessage>(this IServiceCollection services, Action<DiskMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddDiskMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add a <see cref="DiskMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddDiskMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, DiskMessageQueueOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<DiskMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new DiskMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<DiskMessageQueue<TMessage>>>();
                    return new DiskMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
