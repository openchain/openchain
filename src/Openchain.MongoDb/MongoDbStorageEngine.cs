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
                        InitialRecords = new List<MongoDbRecord>()
                    };
                    await PendingTransactionCollection.InsertOneAsync(ptr);

                    // lock records
                    foreach (var r in mutation.Records)
                    {
                        var previous=await LockRecord(transactionHash, r);
                        if (previous != null) ptr.InitialRecords.Add(previous);
                    }

                    // save original records
                    await PendingTransactionCollection.UpdateOneAsync(x => x.TransactionHash.Equals(transactionHash),
                        Builders<MongoDbPendingTransaction>.Update.Set(x => x.InitialRecords, ptr.InitialRecords));

                    // update records
                    foreach (var rec in mutation.Records)
                    {
                        RecordKey key = RecordKey.Parse(rec.Key);
                        var r = new MongoDbRecord { Key = rec.Key.ToByteArray(), KeyS = Encoding.UTF8.GetString(rec.Key.ToByteArray()), Value = rec.Value?.ToByteArray(), Version = rec.Version.ToByteArray(), Path = key.Path.Segments.ToArray(), Type = key.RecordType, Name = key.Name };
                        if (r.Value == null)
                        {
                            var res = await RecordCollection.CountAsync(x => x.Key.Equals(r.Key) && x.Version.Equals(r.Version));
                            if ((r.Version.Length == 0 && res != 0) || (r.Version.Length != 0 && res != 1))
                                throw new ConcurrentMutationException(rec);
                        }
                        else
                        {
                            if (r.Version.Length == 0)
                            {
                                r.Version = mutationHash;
                                await RecordCollection.InsertOneAsync(r);
                            }
                            else
                            {
                                var res = await RecordCollection.UpdateOneAsync(
                                    x => x.Key.Equals(r.Key) && x.Version.Equals(r.Version) && x.TransactionLock.Equals(transactionHash),
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

                    // unlock records
                    foreach (var r in mutation.Records)
                    {
                        await UnlockRecord(transactionHash, r);
                    }

                    // remove pending transaction
                    await PendingTransactionCollection.DeleteOneAsync(x => x.TransactionHash.Equals(transactionHash));
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

        private async Task<MongoDbRecord> LockRecord(byte[] transactionHash, Record r)
        {
            var key = r.Key.ToByteArray();
            var version = r.Version.ToByteArray();
            if (version.Length != 0)
            {
                var res = await RecordCollection.FindOneAndUpdateAsync(
                    x => x.Key.Equals(key) && x.Version.Equals(version) && x.TransactionLock == null,
                    Builders<MongoDbRecord>.Update
                        .Set(x => x.TransactionLock, transactionHash)
                );
                if (res == null)
                    throw new ConcurrentMutationException(r);
                return res;
            }
            return null;
        }

        private async Task UnlockRecord(byte[] transactionHash, Record r)
        {
            var key = r.Key.ToByteArray();
            var version = r.Version.ToByteArray();
            if (version.Length != 0)
            {
                var res = await RecordCollection.UpdateOneAsync(
                    x => x.Key.Equals(key) && x.TransactionLock == transactionHash,
                    Builders<MongoDbRecord>.Update
                        .Unset(x => x.TransactionLock)
                );
                if (res.ModifiedCount != 1 || res.MatchedCount != 1)
                    throw new ConcurrentMutationException(r);
            }
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
                            x => x.Key.Equals(r.Key) && x.TransactionLock.Equals(hash),
                            Builders<MongoDbRecord>.Update.Set(x => x.Value, r.Value).Set(x => x.Version, r.Version).Unset(x => x.TransactionLock)
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
            return res == null ? null : new ByteString(res.RawData);
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
            StartWorkerTaskIfNeeded();
        }
    }
}