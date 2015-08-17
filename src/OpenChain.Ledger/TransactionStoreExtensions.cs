using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public static class TransactionStoreExtensions
    {
        public static async Task<Record> GetRecord(this ITransactionStore store, RecordKey key)
        {
            IList<Record> result = await store.GetRecords(new[] { key.ToBinary() });
            return result[0];
        }

        public static async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccounts(this ITransactionStore store, IEnumerable<AccountKey> accounts)
        {
            IList<Record> records = await store.GetRecords(accounts.Select(account => account.Key.ToBinary()));

            return records.Select(record => AccountStatus.FromRecord(RecordKey.Parse(record.Key), record)).ToDictionary(account => account.AccountKey, account => account);
        }

        public static async Task<AccountStatus> GetAccount(this ITransactionStore store, AccountKey account)
        {
            return (await store.GetAccounts(new[] { account })).First().Value;
        }
    }
}
