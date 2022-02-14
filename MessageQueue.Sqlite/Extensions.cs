using KM.MessageQueue;
using KM.MessageQueue.Sqlite;
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
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new SqliteMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
