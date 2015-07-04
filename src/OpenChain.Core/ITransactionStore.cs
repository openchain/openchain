using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Core
{
    public interface ITransactionStore
    {
        /// <summary>
        /// Adds a record to the ledger.
        /// </summary>
        /// <exception cref="AccountModifiedException">An account has been modified and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation and contains the new ledger hash.</returns>
        Task<BinaryData> AddTransaction(BinaryData rawTransaction, DateTime timestamp, BinaryData externalMetadata);

        /// <summary>
        /// Adds a record to the ledger.
        /// </summary>
        /// <param name="rawLedgerRecord">The serialized ledger record to add to the ledger.</param>
        /// <exception cref="AccountModifiedException">An account has been modified and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation and contains the new ledger hash.</returns>
        Task<BinaryData> AddLedgerRecord(BinaryData rawLedgerRecord);

        Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetAccounts(IEnumerable<AccountKey> accountKeys);

        Task<IReadOnlyList<BinaryData>> GetTransactionStream(BinaryData from);
    }
}
