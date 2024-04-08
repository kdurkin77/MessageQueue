using KM.MessageQueue;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MessageQueueExtensions
    {
        /// <summary>
        /// Adds a queue implentation of type <see cref="IMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/> 
        /// </summary>
        /// <typeparam name="TQueue"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="create"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
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
                .AddSingleton<IReadOnlyMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IWriteOnlyMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                ;
        }

        public static IServiceCollection AddBulkMessageQueue<TQueue, TMessage>(this IServiceCollection services, Func<IServiceProvider, TQueue> create)
            where TQueue : class, IBulkMessageQueue<TMessage>
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
                .AddSingleton<IBulkMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IBulkReadOnlyMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                .AddSingleton<IBulkWriteOnlyMessageQueue<TMessage>, TQueue>(services => services.GetRequiredService<TQueue>())
                ;
        }
    }
}
