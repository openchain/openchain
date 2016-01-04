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
        public bool RunRollbackThread { get; set; } = true;
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
            using (var m = new MongoDbStorageEngine(config, serviceProvider.GetRequiredService<ILogger>()))
            {
                await m.CreateIndexes();
            }
        }
    }
}