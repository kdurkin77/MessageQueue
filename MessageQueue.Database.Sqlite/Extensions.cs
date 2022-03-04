using KM.MessageQueue.Database.Sqlite;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SqliteQueueExtensions
    {
        /// <summary>
        /// Add a <see cref="SqliteMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqliteMessageQueue<TMessage>(this IServiceCollection services, Action<SqliteMessageQueueOptions<TMessage>> configureOptions)
        {
            return services.AddSqliteMessageQueue<TMessage>((_, options) => configureOptions(options));
        }

        /// <summary>
        /// Add a <see cref="SqliteMessageQueue{TMessage}"/> to the specified <see cref="IServiceCollection"/>
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddSqliteMessageQueue<TMessage>(this IServiceCollection services, Action<IServiceProvider, SqliteMessageQueueOptions<TMessage>> configureOptions)
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
                    var options = new SqliteMessageQueueOptions<TMessage>();
                    configureOptions(services, options);

                    var logger = services.GetRequiredService<ILogger<SqliteMessageQueue<TMessage>>>();
                    return new SqliteMessageQueue<TMessage>(logger, Options.Options.Create(options));
                });
        }
    }
}
