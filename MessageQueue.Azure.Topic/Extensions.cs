using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureTopicExtensions
    {
        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<AzureTopicMessageQueueOptions> configureOptions)
        {
            return services.AddAzureTopicMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, AzureTopicMessageQueueOptions> configureOptions)
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
                .AddMessageQueue<AzureTopicMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new AzureTopicMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<AzureTopicMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new AzureTopicMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
