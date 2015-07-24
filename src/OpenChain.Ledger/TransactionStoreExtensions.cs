using OpenChain.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public static class TransactionStoreExtensions
    {
        public static async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccounts(this ITransactionStore store, IEnumerable<AccountKey> accounts)
        {
            IList<Record> records = await store.GetRecords(accounts.Select(account => account.BinaryData));

            return records.Select(AccountStatus.FromRecord).ToDictionary(account => account.AccountKey, account => account);
        }

        public static async Task<AccountStatus> GetAccount(this ITransactionStore store, AccountKey account)
        {
            return (await store.GetAccounts(new[] { account })).First().Value;
        }
    }
}
