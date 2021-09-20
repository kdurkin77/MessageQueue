using KM.MessageQueue;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwarderExtensions
    {
        public static IServiceCollection AddForwarder<TMessage, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<ForwarderOptions> configureOptions)
            where TSourceQueue : IMessageQueue<TMessage>
            where TDestinationQueue : IMessageQueue<TMessage>
        {
            return services.AddForwarder<TMessage, TSourceQueue, TDestinationQueue>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddForwarder<TMessage, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<IServiceProvider, ForwarderOptions> configureOptions)
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
                    var logger = services.GetRequiredService<ILogger<Forwarder<TMessage>>>();
                    var source = services.GetRequiredService<TSourceQueue>();
                    var destination = services.GetRequiredService<TDestinationQueue>();

                    var options = new ForwarderOptions();
                    configureOptions(services, options);
                    return new Forwarder<TMessage>(logger, Options.Options.Create(options), source, destination);
                });
        }
    }
}
