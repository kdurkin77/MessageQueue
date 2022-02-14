using KM.MessageQueue;
using KM.MessageQueue.SQLite;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SQLiteQueueExtensions
    {
        public static IServiceCollection AddSQLiteQueue<TMessage>(this IServiceCollection services, Action<SQLiteQueueOptions> configureOptions)
        {
            return services.AddSQLiteQueue<TMessage>((_, options) => configureOptions(options));
        }

        public static IServiceCollection AddSQLiteQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, SQLiteQueueOptions> configureOptions)
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
                .AddMessageQueue<SQLiteQueue<TMessage>, TMessage>(services =>
                {
                    var options = new SQLiteQueueOptions();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<SQLiteQueue<TMessage>>>();
                    var formatter = services.GetRequiredService<IMessageFormatter<TMessage>>();
                    return new SQLiteQueue<TMessage>(logger, Options.Options.Create(options), formatter);
                });
        }
    }
}
