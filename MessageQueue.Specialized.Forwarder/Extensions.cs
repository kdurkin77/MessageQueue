using KM.MessageQueue;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwarderExtensions
    {
        public static IServiceCollection AddForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessageIn, TMessageOut0>
            where TDestinationQueue : IMessageQueue<TMessageIn, TMessageOut1>
        {
            return services.AddForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1, TSourceQueue, TDestinationQueue>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<IServiceProvider, ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessageIn, TMessageOut0>
            where TDestinationQueue : IMessageQueue<TMessageIn, TMessageOut1>
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
                .AddSingleton<IMessageQueue<TMessageIn, TMessageOut1>>(services =>
                {
                    var logger = services.GetRequiredService<ILogger<ForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1>>>();
                    var source = services.GetRequiredService<TSourceQueue>();
                    var destination = services.GetRequiredService<TDestinationQueue>();

                    var options = new ForwarderMessageQueueOptions();
                    configureOptions(services, options);
                    return new ForwarderMessageQueue<TMessageIn, TMessageOut0, TMessageOut1>(logger, Options.Options.Create(options), source, destination);
                });
        }
    }
}
