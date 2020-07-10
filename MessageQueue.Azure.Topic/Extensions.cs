using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureTopicExtensions
    {
        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<AzureTopicOptions<TMessage>> configureOptions)
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
                .AddSingleton<IMessageQueue<TMessage>, AzureTopic<TMessage>>()
                .Configure(configureOptions)
                ;
        }
    }
}
