using KM.MessageQueue;
using KM.MessageQueue.Azure.Topic;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AzureTopicExtensions
    {
        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<AzureTopicOptions<TMessage>> configureOptions)
        {
            return services.AddAzureTopicMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddAzureTopicMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, AzureTopicOptions<TMessage>> configureOptions)
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
                .AddMessageQueue<AzureTopic<TMessage>, TMessage>(services =>
                {
                    var options = new AzureTopicOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<AzureTopic<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new AzureTopic<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
