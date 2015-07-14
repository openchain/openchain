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
            IList<KeyValuePair> pairs = await store.GetValues(accounts.Select(account => account.BinaryData));

            return pairs.Select(AccountStatus.FromKeyValuePair).ToDictionary(account => account.AccountKey, account => account);
        }

        public static async Task<AccountStatus> GetAccount(this ITransactionStore store, AccountKey account)
        {
            return (await store.GetAccounts(new[] { account })).First().Value;
        }
    }
}
