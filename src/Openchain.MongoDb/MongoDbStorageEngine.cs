using Openchain.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text;
using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Openchain.MongoDb
{
    public class MongoDbStorageEngine : MongoDbBase, IStorageEngine
    {
        internal IMongoCollection<MongoDbRecord> RecordCollection
        {
            get;
            set;
        }

        internal IMongoCollection<MongoDbTransaction> TransactionCollection
        {
            get;
            set;
        }

        internal IMongoCollection<MongoDbPendingTransaction> PendingTransactionCollection
        {
            get;
            set;
        }

        protected MongoDbStorageEngineConfiguration Configuration { get; }

        protected ILogger Logger { get; }

        public MongoDbStorageEngine(MongoDbStorageEngineConfiguration config, ILogger logger): base(config.ConnectionString, config.Database)
        {
            Configuration = config;
            Logger = logger;
            RecordCollection = Database.GetCollection<MongoDbRecord>("records");
            TransactionCollection = Database.GetCollection<MongoDbTransaction>("transactions");
            PendingTransactionCollection = Database.GetCollection<MongoDbPendingTransaction>("pending_transactions");
        }
        
        public async Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            List<byte[]> transactionHashes = new List<byte[]>();
            List<Record> lockedRecords = new List<Record>();
            byte[] lockToken = Guid.NewGuid().ToByteArray();
            try {
                foreach (ByteString rawTransaction in transactions)
                {
                    byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);
                    byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);
                    List<byte[]> records = new List<byte[]>();

                    transactionHashes.Add(transactionHash);

                    // add pending transaction
                    var ptr = new MongoDbPendingTransaction
                    {
                        MutationHash = mutationHash,
                        TransactionHash = transactionHash,
                        RawData = rawTransactionBuffer,
                        LockTimestamp = DateTime.UtcNow,
                        InitialRecords = new List<MongoDbRecord>(),
                        AddedRecords = new List<byte[]>(),
                        LockToken = lockToken
                    };
                    await PendingTransactionCollection.InsertOneAsync(ptr);

                    // lock records
                    foreach (var r in mutation.Records)
                    {
                        var previous = await LockRecord(lockToken, r);
                        if (previous != null)
                        {
                            ptr.InitialRecords.Add(previous);
                            lockedRecords.Add(r);
                        }
                        else
                            if (r.Value != null)
                            {
                                ptr.AddedRecords.Add(r.Key.ToByteArray());
                                lockedRecords.Add(r);
                            }
                    }

                    // save original records
                    await PendingTransactionCollection.UpdateOneAsync(
                        x => x.TransactionHash.Equals(transactionHash),
                        Builders<MongoDbPendingTransaction>.Update
                            .Set(x => x.InitialRecords, ptr.InitialRecords)
                            .Set(x => x.AddedRecords, ptr.AddedRecords)
                    );

                    // update records
                    foreach (var rec in mutation.Records)
                    {
                        MongoDbRecord r = BuildMongoDbRecord(rec);
                        if (r.Value == null)
                        {
                            if (r.Version.Length == 0) // No record expected
                            {
                                var res = await RecordCollection.CountAsync(x => x.Key.Equals(r.Key));
                                if (res != 0) // a record exists
                                    throw new ConcurrentMutationException(rec);
                            }
                            else
                            {   // specific version expected
                                var res = await RecordCollection.CountAsync(x => x.Key.Equals(r.Key) && x.Version.Equals(r.Version));
                                if (res != 1) // expected version not found
                                    throw new ConcurrentMutationException(rec);
                            }
                        }
                        else
                        {
                            if (r.Version.Length == 0)
                            {
                                r.Version = mutationHash;
                                r.TransactionLock = lockToken;
                                try {
                                    await RecordCollection.InsertOneAsync(r);
                                } catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) {
                                    throw new ConcurrentMutationException(rec);
                                }
                            }
                            else
                            {
                                var res = await RecordCollection.UpdateOneAsync(
                                    x => x.Key.Equals(r.Key) && x.Version.Equals(r.Version) && x.TransactionLock.Equals(lockToken),
                                    Builders<MongoDbRecord>.Update
                                        .Set(x => x.Value, r.Value)
                                        .Set(x => x.Version, mutationHash)
                                );
                                if (res.MatchedCount != 1 || res.ModifiedCount != 1)
                                    throw new ConcurrentMutationException(rec);
                            }
                            records.Add(r.Key);
                        }
                    }

                    // add transaction
                    var tr = new MongoDbTransaction {
                        MutationHash = mutationHash,
                        TransactionHash = transactionHash,
                        RawData = rawTransactionBuffer,
                        Records = records
                    };
                    await TransactionCollection.InsertOneAsync(tr);
                }

                // unlock records
                List<ByteString> l = new List<ByteString>();
                foreach (var r in lockedRecords)
                {
                    if (!l.Contains(r.Key))
                    {
                        await UnlockRecord(lockToken, r);
                        l.Add(r.Key);
                    }
                }

                // remove pending transaction
                foreach (var hash in transactionHashes)
                {
                    await PendingTransactionCollection.DeleteOneAsync(x => x.TransactionHash.Equals(hash));
                }
            } catch (Exception ex1)
            {
                foreach (var hash in transactionHashes)
                {
                    try {
                        await RollbackTransaction(hash);
                    } catch (Exception ex2)
                    {
                        throw new AggregateException(ex2, ex1);
                    }
                }
                throw;
            }
        }

        protected virtual MongoDbRecord BuildMongoDbRecord(Record rec)
        {
            var r = new MongoDbRecord { Key = rec.Key.ToByteArray(),
                KeyS = Encoding.UTF8.GetString(rec.Key.ToByteArray()),
                Value = rec.Value?.ToByteArray(),
                Version = rec.Version.ToByteArray()
            };
            return r;
        }

        private async Task<MongoDbRecord> LockRecord(byte[] lockToken, Record r)
        {
            var key = r.Key.ToByteArray();
            var version = r.Version.ToByteArray();
            if (version.Length != 0)
            {
                var res = await RecordCollection.FindOneAndUpdateAsync(
                    x => x.Key.Equals(key) && x.Version.Equals(version) && (x.TransactionLock == null || x.TransactionLock.Equals(lockToken)),
                    Builders<MongoDbRecord>.Update
                        .Set(x => x.TransactionLock, lockToken)
                );
                if (res == null)
                    throw new ConcurrentMutationException(r);
                return res;
            }
            return null;
        }

        private async Task UnlockRecord(byte[] lockToken, Record r)
        {
            var key = r.Key.ToByteArray();
            var res = await RecordCollection.UpdateOneAsync(
                x => x.Key.Equals(key) && x.TransactionLock == lockToken,
                Builders<MongoDbRecord>.Update
                    .Unset(x => x.TransactionLock)
            );
            if (res.ModifiedCount != 1 || res.MatchedCount != 1)
                throw new ConcurrentMutationException(r);
        }
        
        private async Task RollbackTransaction(byte[] hash)
        {
            // Rollback is idempotent && reentrant : may be call twice even at the same time
            try
            {
                // get affected records
                var trn = await PendingTransactionCollection.Find(x => x.TransactionHash.Equals(hash)).SingleOrDefaultAsync();

                if (trn != null)
                {

                    // revert records values & version
                    foreach (var r in trn.InitialRecords)
                    {
                        await RecordCollection.FindOneAndUpdateAsync(
                            x => x.Key.Equals(r.Key) && x.TransactionLock.Equals(trn.LockToken),
                            Builders<MongoDbRecord>.Update.Set(x => x.Value, r.Value).Set(x => x.Version, r.Version).Unset(x => x.TransactionLock)
                        );
                    }

                    foreach (var r in trn.AddedRecords)
                    {
                        await RecordCollection.FindOneAndDeleteAsync(
                            x => x.Key.Equals(r) && x.TransactionLock.Equals(trn.LockToken)
                        );
                    }

                    // remove transaction
                    await TransactionCollection.DeleteOneAsync(x => x.TransactionHash.Equals(hash));

                    // remove pending transaction
                    await PendingTransactionCollection.DeleteOneAsync(x => x.TransactionHash.Equals(hash));
                }

            }
            catch (Exception ex)
            {
                var msg = "Error rollbacking transaction : " + new ByteString(hash).ToString();
                Logger.LogCritical(msg, ex);
                throw new Exception(msg, ex);
            }
        }

        public async Task RollbackAllPendingTransactions(DateTime limit)
        {
            var res = await PendingTransactionCollection.Find(x => x.LockTimestamp < limit).ToListAsync();
            foreach (var t in res)
                await RollbackTransaction(t.TransactionHash);
        }

        public async Task<ByteString> GetLastTransaction()
        {
            var res = await TransactionCollection.Find(x => true).SortByDescending(x => x.Timestamp).FirstOrDefaultAsync();
            return res == null ? ByteString.Empty : new ByteString(res.TransactionHash);
        }

        public async Task<IReadOnlyList<Record>> GetRecords(IEnumerable<ByteString> keys)
        {
            var list = new List<Record>();
            foreach (var k in keys)
            {
                var replay = false;
                var retryCount = Configuration.ReadRetryCount;
                do
                {
                    var cmpkey = k.ToByteArray();
                    var r = await RecordCollection.Find(x => x.Key == cmpkey).FirstOrDefaultAsync();
                    if (r == null)
                        list.Add(new Record(k, ByteString.Empty, ByteString.Empty));
                    else
                    {
                        // retry
                        if (r.TransactionLock != null)
                        {
                            if (retryCount <= 0)
                                throw new Exception("Lock timeout reading record " + cmpkey.ToString());
                            await Task.Delay(Configuration.ReadLoopDelay);
                            replay = true;
                            retryCount--;
                        }
                        else
                        {
                            list.Add(new Record(k, new ByteString(r.Value), new ByteString(r.Version)));
                        }
                    }
                } while (replay);
            }

            return list.AsReadOnly();
        }

        public async Task<IReadOnlyList<ByteString>> GetTransactions(ByteString from)
        {
            MongoDbTransaction res = null;
            if (from != null)
            {
                var cmpkey = from.ToByteArray();
                res = await TransactionCollection.Find(x => x.TransactionHash == cmpkey).FirstOrDefaultAsync();
            }
            List<MongoDbTransaction> l;
            if (res == null)
            {
                l = await TransactionCollection.Find(x => true).SortBy(x => x.Timestamp).ToListAsync();
            }
            else
            {
                BsonTimestamp ts = res.Timestamp;
                l = await TransactionCollection.Find(x => x.Timestamp > ts).SortBy(x => x.Timestamp).ToListAsync();
            }

            return l.Select(x => new ByteString(x.RawData)).ToList().AsReadOnly();
        }

        public void RollbackWorker()
        {
            while (true)
            {
                try {
                    RollbackAllPendingTransactions(DateTime.UtcNow - Configuration.StaleTransactionDelay).Wait();
                } catch (Exception ex)
                {
                    Logger.LogCritical("Rollback failed in worker", ex);
                }
                Task.Delay(Configuration.StaleTransactionDelay);
            }
        }

        static Task rollbackWorkerTask { get; set; }
        static object _lock = new object();
        public void StartWorkerTaskIfNeeded()
        {
            if (rollbackWorkerTask == null || rollbackWorkerTask.IsFaulted)
            {
                lock (_lock)
                {
                    if (rollbackWorkerTask == null || rollbackWorkerTask.IsFaulted)
                    {
                        rollbackWorkerTask = new TaskFactory().StartNew(RollbackWorker);
                    }
                }
            }
        }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Initialize()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (Configuration.RunRollbackThread)
                StartWorkerTaskIfNeeded();
        }

        internal async Task CreateIndexes()
        {
            await TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.Timestamp), new CreateIndexOptions { Background = true, Unique = true });
            await TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.MutationHash), new CreateIndexOptions { Background = true, Unique = true });
            await TransactionCollection.Indexes.CreateOneAsync(Builders<MongoDbTransaction>.IndexKeys.Ascending(x => x.Records), new CreateIndexOptions { Background = true, Unique = false });
            await RecordCollection.Indexes.CreateOneAsync(Builders<MongoDbRecord>.IndexKeys.Ascending(x => x.Type).Ascending(x => x.Name), new CreateIndexOptions { Background = true });
            await RecordCollection.Indexes.CreateOneAsync(Builders<MongoDbRecord>.IndexKeys.Ascending(x => x.KeyS), new CreateIndexOptions { Background = true });
        }

    }
}