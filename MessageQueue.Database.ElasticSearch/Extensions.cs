using KM.MessageQueue;
using KM.MessageQueue.Database.ElasticSearch;
using KM.MessageQueue.Formatters.ObjectToJsonObject;
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
                .AddMessageQueue<ElasticSearchMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new ElasticSearchMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<ElasticSearchMessageQueue<TMessage>>>();
                    var formatter = new JsonObjectFormatter<TMessage>();
                    return new ElasticSearchMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }

        public static IServiceCollection AddElasticSearchMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<ElasticSearchMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, JObject>
        {
            return services.AddElasticSearchMessageQueue<TMessage, TFormatter>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddElasticSearchMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<IServiceProvider, ElasticSearchMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, JObject>
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
                .AddMessageQueue<ElasticSearchMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new ElasticSearchMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<ElasticSearchMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<TFormatter>();
                    return new ElasticSearchMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
