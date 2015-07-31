using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface ILedgerQueries
    {
        Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccount(string account);

        Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetSubaccounts(string rootAccount);

        Task<BinaryData> GetTransaction(BinaryData mutationHash);
    }
}
