using Openchain.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text;

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

        public MongoDbStorageEngine(string connectionString, string database): base (connectionString, database)
        {
            RecordCollection = Database.GetCollection<MongoDbRecord>("records");
            TransactionCollection = Database.GetCollection<MongoDbTransaction>("transactions");
        }

        public async Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            foreach (ByteString rawTransaction in transactions)
            {
                byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);
                byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);
                List<byte[]> records = new List<byte[]>();
                foreach (var rec in mutation.Records)
                {
                    RecordKey key = RecordKey.Parse(rec.Key);
                    var r = new MongoDbRecord{Key = rec.Key.ToByteArray(), KeyS = Encoding.UTF8.GetString(rec.Key.ToByteArray()), Value = rec.Value?.ToByteArray(), Version = rec.Version.ToByteArray(), Path = key.Path.Segments.ToArray(), Type = key.RecordType, Name = key.Name};
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
                                x => x.Key.Equals(r.Key) && x.Version.Equals(r.Version), 
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

                var tr = new MongoDbTransaction{
                    MutationHash = mutationHash,
                    TransactionHash = transactionHash,
                    RawData = rawTransactionBuffer,
                    Records = records
                };
                await TransactionCollection.InsertOneAsync(tr);
            }
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
                var cmpkey = k.ToByteArray();
                var r = await RecordCollection.Find(x => x.Key == cmpkey).FirstOrDefaultAsync();
                if (r == null)
                    list.Add(new Record(k, ByteString.Empty, ByteString.Empty));
                else
                    list.Add(new Record(k, new ByteString(r.Value), new ByteString(r.Version)));
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Initialize()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
        }
    }
}