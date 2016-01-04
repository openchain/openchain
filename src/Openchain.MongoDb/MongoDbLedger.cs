using Openchain.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Openchain.MongoDb
{
    public class MongoDbLedger : MongoDbStorageEngine, ILedgerQueries, ILedgerIndexes
    {
        public MongoDbLedger(MongoDbStorageEngineConfiguration config, ILogger logger): base(config, logger)
        {
        }

        public async Task<IReadOnlyList<Record>> GetAllRecords(RecordType type, string name)
        {
            var list = new List<Record>();
            var res = await RecordCollection.FindAsync(x => x.Type == type && x.Name == name);
            await res.ForEachAsync(r =>
            {
                list.Add(new Record(new ByteString(r.Key), new ByteString(r.Value), new ByteString(r.Version)));
            }

            );
            return list.AsReadOnly();
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
        {
            var prefixS = Encoding.UTF8.GetString(prefix.ToByteArray());
            var list = new List<Record>();
            var res = await RecordCollection.FindAsync(x => x.KeyS.StartsWith(prefixS));
            await res.ForEachAsync(r =>
            {
                list.Add(new Record(new ByteString(r.Key), new ByteString(r.Value), new ByteString(r.Version)));
            }

            );
            return list.AsReadOnly();
        }

        public async Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey)
        {
            var key = recordKey.ToByteArray();
            var res = await TransactionCollection.Find(Builders<MongoDbTransaction>.Filter.AnyEq(x=>x.Records, key))
                .Project(x=>x.MutationHash)
                .SortBy(x=>x.Timestamp)
                .ToListAsync();
            return res.Select(x=>new ByteString(x)).ToList().AsReadOnly();
        }

        public async Task<ByteString> GetTransaction(ByteString mutationHash)
        {
            var key = mutationHash.ToByteArray();
            var res = await TransactionCollection.Find(x => x.MutationHash == key).SingleOrDefaultAsync();
            return res == null ? null : new ByteString(res.RawData);
        }
    }
}