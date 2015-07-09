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
        /// <param name="rawLedgerRecord">The serialized ledger record to add to the ledger.</param>
        /// <exception cref="AccountModifiedException">An account has been modified and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation and contains the hash of the record.</returns>
        Task<BinaryData> AddLedgerRecord(BinaryData rawLedgerRecord);

        /// <summary>
        /// Gets the hash of the last record in the ledger.
        /// </summary>
        /// <returns>A task that represents the completion of the operation and contains the hash of the last record.</returns>
        Task<BinaryData> GetLastRecord();

        /// <summary>
        /// Gets the leger records following the one whose hash has been provided.
        /// </summary>
        /// <param name="from">The hash of the record to start streaming from.</param>
        /// <returns>An observable representing the transaction stream.</returns>
        IObservable<BinaryData> GetRecordStream(BinaryData from);
    }
}
