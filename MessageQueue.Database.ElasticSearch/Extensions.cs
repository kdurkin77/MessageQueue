using KM.MessageQueue;
using KM.MessageQueue.Database.ElasticSearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElasticDatabaseExtensions
    {
        public static IServiceCollection AddElasticSearchMessageQueue<TMessage>(this IServiceCollection services, Action<ElasticSearchMessageQueueOptions> configureOptions)
        {
            return services.AddElasticSearchMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddElasticSearchMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, ElasticSearchMessageQueueOptions> configureOptions)
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
                .AddMessageQueue<ElasticSearchMessageQueue<TMessage>, TMessage, JObject>(services =>
                {
                    var options = new ElasticSearchMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<ElasticSearchMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage, JObject>>();
                    return new ElasticSearchMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
