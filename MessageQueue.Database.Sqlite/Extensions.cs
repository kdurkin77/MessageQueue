using KM.MessageQueue;
using KM.MessageQueue.Database.Sqlite;
using KM.MessageQueue.Formatters.ToJsonString;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqliteQueueExtensions
    {
        public static IServiceCollection AddSqliteMessageQueue<TMessage>(this IServiceCollection services, Action<SqliteMessageQueueOptions> configureOptions)
        {
            return services.AddSqliteMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddSqliteMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, SqliteMessageQueueOptions> configureOptions)
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
                .AddMessageQueue<SqliteMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new SqliteMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<SqliteMessageQueue<TMessage>>>();
                    var formatter = new JsonStringFormatter<TMessage>();
                    return new SqliteMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }

        public static IServiceCollection AddSqliteMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<SqliteMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, string>
        {
            return services.AddSqliteMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddSqliteMessageQueue<TMessage, TFormatter>(this IServiceCollection services, Action<IServiceProvider, SqliteMessageQueueOptions> configureOptions)
            where TFormatter : class, IMessageFormatter<TMessage, string>
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
                .AddMessageQueue<SqliteMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new SqliteMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<SqliteMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<TFormatter>();
                    return new SqliteMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
