using KM.MessageQueue;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessageQueueExtensions
    {
        public static IServiceCollection AddMessageQueue<TQueue, TMessageIn, TMessageOut>(this IServiceCollection services, Func<IServiceProvider, TQueue> create)
            where TQueue : class, IMessageQueue<TMessageIn, TMessageOut>
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (create is null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            return services
                .AddSingleton(create)
                .AddSingleton<IMessageQueue<TMessageIn, TMessageOut>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IReadOnlyMessageQueue<TMessageIn, TMessageOut>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IWriteOnlyMessageQueue<TMessageIn>, TQueue>(services => services.GetRequiredService<TQueue>())
                ;
        }
    }
}
