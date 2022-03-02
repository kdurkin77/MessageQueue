using Elasticsearch.Net;
using Nest;
using System;
using System.Collections.Generic;

namespace KM.MessageQueue.Database.ElasticSearch
{
    public sealed class ElasticSearchMessageQueueOptions
    {
        public ConnectionSettings ConnectionSettings { get; private set; } = new ConnectionSettings();
        public void UseConnectionUri(Uri uri, Action<ConnectionSettings> configureSettings)
        {
            ConnectionSettings = new ConnectionSettings(uri);
            configureSettings(ConnectionSettings);
        }

        public void UseConnectionPool(IEnumerable<Uri> uris, Action<ConnectionSettings> configureSettings)
        {
            var connPool = new SniffingConnectionPool(uris);
            ConnectionSettings = new ConnectionSettings(connPool);
            configureSettings(ConnectionSettings);
        }
    }
}
