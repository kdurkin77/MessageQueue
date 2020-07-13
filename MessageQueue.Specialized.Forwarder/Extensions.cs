using KM.MessageQueue;
using KM.MessageQueue.Specialized.Forwarder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ForwarderExtensions
    {
        public static IServiceCollection AddForwarder<TMessage, TSourceQueue, TDestinationQueue>(this IServiceCollection services, Action<ForwarderOptions<TMessage>> configureOptions)
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
                    var options = services.GetRequiredService<IOptions<ForwarderOptions<TMessage>>>();
                    var source = services.GetRequiredService<TSourceQueue>();
                    var destination = services.GetRequiredService<TDestinationQueue>();

                    return new Forwarder<TMessage>(logger, options, source, destination);
                })
                .Configure(configureOptions)
                ;
        }
    }
}
