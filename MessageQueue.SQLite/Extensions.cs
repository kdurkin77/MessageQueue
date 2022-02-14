using KM.MessageQueue;
using KM.MessageQueue.SQLite;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SQLiteQueueExtensions
    {
        public static IServiceCollection AddSQLiteMessageQueue<TMessage>(this IServiceCollection services, Action<SQLiteMessageQueueOptions> configureOptions)
        {
            return services.AddSQLiteMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddSQLiteMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, SQLiteMessageQueueOptions> configureOptions)
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
                .AddMessageQueue<SQLiteMessageQueue<TMessage>, TMessage>(services =>
                {
                    var options = new SQLiteMessageQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<SQLiteMessageQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new SQLiteMessageQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
