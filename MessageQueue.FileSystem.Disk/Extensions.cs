using KM.MessageQueue;
using KM.MessageQueue.FileSystem.Disk;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiskMessageQueueExtensions
    {
        public static IServiceCollection AddDiskMessageQueue<TMessage>(this IServiceCollection services, Action<DiskMessageQueueOptions> configureOptions)
        {
            return services.AddDiskMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddDiskMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, DiskMessageQueueOptions> configureOptions)
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
                    var options = new DiskMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<DiskMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new DiskMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
