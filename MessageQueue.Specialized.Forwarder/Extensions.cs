using KM.MessageQueue;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwarderExtensions
    {
        public static IServiceCollection AddForwarderMessageQueue<TMessageIn, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessageIn>
            where TDestinationQueue : IMessageQueue<TMessageIn>
        {
            return services.AddForwarderMessageQueue<TMessageIn, TSourceQueue, TDestinationQueue>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddForwarderMessageQueue<TMessageIn, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<IServiceProvider, ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessageIn>
            where TDestinationQueue : IMessageQueue<TMessageIn>
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
                .AddSingleton<IMessageQueue<TMessageIn>>(services =>
                {
                    var logger = services.GetRequiredService<ILogger<ForwarderMessageQueue<TMessageIn>>>();
                    var source = services.GetRequiredService<TSourceQueue>();
                    var destination = services.GetRequiredService<TDestinationQueue>();

                    var options = new ForwarderMessageQueueOptions();
                    configureOptions(services, options);
                    return new ForwarderMessageQueue<TMessageIn>(logger, Options.Options.Create(options), source, destination);
                });
        }
    }
}
