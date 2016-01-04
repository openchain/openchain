using Openchain.Infrastructure;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Openchain.MongoDb
{
    public class MongoDbStorageEngineConfiguration
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public TimeSpan ReadLoopDelay { get; set; }
        public int ReadRetryCount { get; set; }
        public TimeSpan StaleTransactionDelay { get; set; }
    }
    public class MongoDbStorageEngineBuilder : IComponentBuilder<MongoDbLedger>
    {
        public string Name { get; } = "MongoDb";

        MongoDbStorageEngineConfiguration config { get; set; }

        public MongoDbLedger Build(IServiceProvider serviceProvider)
        {
            return new MongoDbLedger(config, serviceProvider.GetRequiredService<ILogger>());
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            config = new MongoDbStorageEngineConfiguration
            {
                ConnectionString = configuration["connection_string"],
                Database = configuration["database"] ?? "openchain",
            };
            var s = configuration["stale_transaction_delay"] ?? "00:01:00";
            config.StaleTransactionDelay = TimeSpan.Parse(s);
            using (var m = new MongoDbLedger(config, serviceProvider.GetRequiredService<ILogger>()))
            {
                await m.TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.Timestamp), new CreateIndexOptions{Background = true, Unique = true});
                await m.TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.MutationHash), new CreateIndexOptions{Background = true, Unique = true});
                await m.TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.Records), new CreateIndexOptions { Background = true, Unique = false });
                await m.RecordCollection.Indexes.CreateOneAsync(Builders<MongoDbRecord>.IndexKeys.Ascending(x => x.Type).Ascending(x => x.Name), new CreateIndexOptions{Background = true});
                await m.RecordCollection.Indexes.CreateOneAsync(Builders<MongoDbRecord>.IndexKeys.Ascending(x => x.KeyS), new CreateIndexOptions{Background = true});
            }
        }
    }
}