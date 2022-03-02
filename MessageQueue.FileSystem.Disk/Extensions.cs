using KM.MessageQueue;
using KM.MessageQueue.FileSystem.Disk;
using KM.MessageQueue.Formatters.ToJObject;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
                    var formatter = new JObjectFormatter<TMessage>();
                    return new DiskMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }

        public static IServiceCollection AddDiskMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<DiskMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, JObject>
        {
            return services.AddDiskMessageQueue<TMessage, TFormatter>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddDiskMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<IServiceProvider, DiskMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, JObject>
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
                    var formatter = services.GetRequiredService<TFormatter>();
                    return new DiskMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
