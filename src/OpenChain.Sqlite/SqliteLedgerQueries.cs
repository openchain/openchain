using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenChain.Ledger;

namespace OpenChain.Sqlite
{
    public class SqliteLedgerQueries : SqliteTransactionStore, ILedgerQueries
    {
        public SqliteLedgerQueries(string filename)
            : base(filename)
        {
        }

        public override async Task EnsureTables()
        {
            await base.EnsureTables();

            await ExecuteAsync(
                "ALTER TABLE Records ADD COLUMN Asset TEXT;",
                new Dictionary<string, object>());
        }

        protected override async Task AddTransaction(Mutation mutation, byte[] mutationHash)
        {
            foreach (Record record in mutation.Records)
            {
                RecordKey key = RecordKey.Parse(record.Key);
                if (key.RecordType == RecordType.Account)
                {
                    await ExecuteAsync(@"
                        UPDATE  Records
                        SET     Asset = @asset
                        WHERE   Key = @key",
                    new Dictionary<string, object>()
                    {
                        ["@key"] = record.Key.ToByteArray(),
                        ["@asset"] = key.AdditionalKeyComponents[0].FullPath
                    });
                }
            }
        }

        public async Task<ByteString> GetTransaction(ByteString mutationHash)
        {
            IEnumerable<ByteString> transactions = await ExecuteAsync(@"
                    SELECT  RawData
                    FROM    Transactions
                    WHERE   MutationHash = @mutationHash",
               reader => new ByteString((byte[])reader.GetValue(0)),
               new Dictionary<string, object>()
               {
                   ["@mutationHash"] = mutationHash.ToByteArray()
               });

            return transactions.FirstOrDefault();
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
        {
            byte[] from = prefix.ToByteArray();
            byte[] to = prefix.ToByteArray();

            if (to[to.Length - 1] < 255)
                to[to.Length - 1] += 1;

            return await ExecuteAsync(@"
                    SELECT  Key, Value, Version
                    FROM    Records
                    WHERE   Key >= @from AND Key < @to",
            reader => new Record(
                    new ByteString((byte[])reader.GetValue(0)),
                    reader.GetValue(1) == null ? ByteString.Empty : new ByteString((byte[])reader.GetValue(1)),
                    new ByteString((byte[])reader.GetValue(2))),
                new Dictionary<string, object>()
                {
                    ["@from"] = from,
                    ["@to"] = to
                });
        }
    }
}
