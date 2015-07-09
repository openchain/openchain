using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Core
{
    /// <summary>
    /// Represents a data store for ledger records.
    /// </summary>
    public interface ILedgerStore
    {
        /// <summary>
        /// Adds a record to the ledger.
        /// </summary>
        /// <exception cref="AccountModifiedException">An account has been modified and the transaction is no longer valid.</exception>
        /// <param name="rawTransaction"></param>
        /// <param name="timestamp"></param>
        /// <param name="externalMetadata"></param>
        /// <returns>A task that represents the completion of the operation and contains the new ledger hash.</returns>
        Task<BinaryData> AddTransaction(BinaryData rawTransaction, DateTime timestamp, BinaryData externalMetadata);

        /// <summary>
        /// Adds a record to the ledger.
        /// </summary>
        /// <param name="rawLedgerRecord">The serialized ledger record to add to the ledger.</param>
        /// <exception cref="AccountModifiedException">An account has been modified and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation and contains the new ledger hash.</returns>
        Task<BinaryData> AddLedgerRecord(BinaryData rawLedgerRecord);

        /// <summary>
        /// Gets the leger records following the one whose hash has been provided.
        /// </summary>
        /// <param name="from">The hash of the record to start streaming from.</param>
        /// <returns>An observable representing the transaction stream.</returns>
        IObservable<BinaryData> GetTransactionStream(BinaryData from);
    }
}
