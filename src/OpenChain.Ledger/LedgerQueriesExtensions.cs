using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public static class LedgerQueriesExtensions
    {
        public static async Task<IReadOnlyList<AccountStatus>> GetAccount(this ILedgerQueries queries, string account)
        {
            ByteString prefix = new ByteString(Encoding.UTF8.GetBytes(account + ":ACC:"));
            IReadOnlyList<Record> records = await queries.GetKeyStartingFrom(prefix);

            return records
                .Select(record => AccountStatus.FromRecord(RecordKey.Parse(record.Key), record))
                .ToList()
                .AsReadOnly();
        }
        
        public static async Task<IReadOnlyList<Record>> GetSubaccounts(this ILedgerQueries queries, string rootAccount)
        {
            ByteString prefix = new ByteString(Encoding.UTF8.GetBytes(rootAccount));
            return await queries.GetKeyStartingFrom(prefix);
        }
    }
}
