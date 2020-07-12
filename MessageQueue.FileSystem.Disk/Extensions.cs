using KM.MessageQueue;
using KM.MessageQueue.FileSystem.Disk;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiskMessageQueueExtensions
    {
        public static IServiceCollection AddDiskMessageQueue<TMessage>(this IServiceCollection services, Action<DiskMessageQueueOptions<TMessage>> configureOptions)
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
                .AddSingleton<IMessageQueue<TMessage>, DiskMessageQueue<TMessage>>()
                .Configure(configureOptions)
                ;
        }
    }
}
