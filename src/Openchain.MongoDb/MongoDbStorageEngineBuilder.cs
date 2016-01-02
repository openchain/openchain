using Openchain.Infrastructure;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Openchain.MongoDb
{
    public class MongoDbStorageEngineBuilder : IComponentBuilder<MongoDbLedger>
    {
        public string Name { get; } = "MongoDb";

        string connectionString
        {
            get;
            set;
        }

        string database
        {
            get;
            set;
        }

        public MongoDbLedger Build(IServiceProvider serviceProvider)
        {
            return new MongoDbLedger(connectionString, database);
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            connectionString = configuration["connection_string"];
            database = configuration["database"] ?? "openchain";
            using (var m = new MongoDbLedger(connectionString, database))
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