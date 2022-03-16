using KM.MessageQueue;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwarderMessageQueueExtensions
    {
        public static IServiceCollection AddForwarderMessageQueue<TMessage, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessage>
            where TDestinationQueue : IMessageQueue<TMessage>
        {
            return services.AddForwarderMessageQueue<TMessage, TSourceQueue, TDestinationQueue>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddForwarderMessageQueue<TMessage, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<IServiceProvider, ForwarderMessageQueueOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessage>
            where TDestinationQueue : IMessageQueue<TMessage>
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
                .AddSingleton<IMessageQueue<TMessage>>(services =>
                {
                    var logger = services.GetRequiredService<ILogger<ForwarderMessageQueue<TMessage>>>();
                    var source = services.GetRequiredService<TSourceQueue>();
                    var destination = services.GetRequiredService<TDestinationQueue>();

                    var options = new ForwarderMessageQueueOptions();
                    configureOptions(services, options);
                    return new ForwarderMessageQueue<TMessage>(logger, Options.Options.Create(options), source, destination);
                });
        }
    }
}
