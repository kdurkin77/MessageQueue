using KM.MessageQueue;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessageQueueExtensions
    {
        public static IServiceCollection AddMessageQueue<TQueue, TMessage>(this IServiceCollection services, Func<IServiceProvider, TQueue> create)
            where TQueue : class, IMessageQueue<TMessage>
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
                .AddSingleton<IMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IReadOnlyQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IWriteOnlyQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                ;
        }
    }
}
