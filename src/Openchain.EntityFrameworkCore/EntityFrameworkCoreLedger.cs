using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Openchain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class EntityFrameworkCoreLedger<TContext> : EntityFrameworkCoreStorageEngine<TContext>, ILedgerQueries, ILedgerIndexes where TContext : DbContext
    {
        private readonly ILogger _logger;

        public EntityFrameworkCoreLedger(TContext context, ILoggerFactory loggerFactory) : base(context, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EntityFrameworkCoreLedger<TContext>>();
        }

        public async Task<IReadOnlyList<Record>> GetAllRecords(RecordType type, string name)
        {
            var records = await Records.Where(r => r.Name == name && r.Type == type).ToListAsync();

            return records.Select(r =>
            {
                return new Record(new ByteString(r.Key), r.Value != null ? new ByteString(r.Value) : ByteString.Empty, new ByteString(r.Version));
            }).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
        {
            byte[] from = prefix.ToByteArray();
            byte[] to = prefix.ToByteArray();

            if (to[to.Length - 1] < 255)
                to[to.Length - 1] += 1;

            var fromString = Encoding.UTF8.GetString(from);
            var toString = Encoding.UTF8.GetString(to);

            var records = await Records.Where(r => Encoding.UTF8.GetString(r.Key).StartsWith(fromString)).ToListAsync();

            return records.Select(r =>
            {
                return new Record(new ByteString(r.Key), r.Value != null ? new ByteString(r.Value) : ByteString.Empty, new ByteString(r.Version));


            }).ToList().AsReadOnly();
        }

        public async Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey)
        {
            var mutations = await RecordMutations.Where(m => m.RecordKey == recordKey.ToByteArray()).ToListAsync();

            return mutations.Select(m =>
            {
                return new ByteString(m.MutationHash);
            }).ToList().AsReadOnly();
        }

        public async Task<ByteString> GetTransaction(ByteString mutationHash)
        {
            var transaction = await Transactions.Where(t => t.MutationHash == mutationHash.ToByteArray()).FirstOrDefaultAsync();

            return new ByteString(transaction.RawData);
        }

        protected override async Task AddTransaction(long transactionId, byte[] mutationHash, Mutation mutation)
        {
            foreach (Record record in mutation.Records)
            {
                RecordKey key = RecordKey.Parse(record.Key);

                var dbRecord = await Records.Where(r => r.Key == record.Key.ToByteArray()).FirstOrDefaultAsync();

                //TODO: check if dbrecord is null

                dbRecord.Type = key.RecordType;
                dbRecord.Name = key.Name;

                Context.Update(dbRecord);

                await Context.SaveChangesAsync();

                var newMutation = new Models.RecordMutation
                {
                    RecordKey = record.Key.ToByteArray(),
                    TransactionId = transactionId,
                    MutationHash = mutationHash
                };

                RecordMutations.Add(newMutation);

                await Context.SaveChangesAsync();
            }
        }
    }
}
