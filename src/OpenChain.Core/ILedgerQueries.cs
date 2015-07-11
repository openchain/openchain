using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Core
{
    public interface ILedgerQueries
    {
        Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetAccounts(IEnumerable<AccountKey> accountKeys);

        Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetAccount(string account);

        Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetSubaccounts(string rootAccount);
    }
}
