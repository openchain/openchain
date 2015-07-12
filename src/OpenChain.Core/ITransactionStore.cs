using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Core
{
    /// <summary>
    /// Represents a data store for ledger records.
    /// </summary>
    public interface ITransactionStore
    {
        /// <summary>
        /// Adds a transaction to the store.
        /// </summary>
        /// <param name="transactions">A collection of serialized transactions to add to the store.</param>
        /// <exception cref="ConcurrentMutationException">A key-value pair has been mutated and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation.</returns>
        Task AddTransactions(IEnumerable<BinaryData> transactions);

        /// <summary>
        /// Gets the hash of the last transaction in the ledger.
        /// </summary>
        /// <returns>A task that represents the completion of the operation and contains the hash of the last transaction.</returns>
        Task<BinaryData> GetLastTransaction();

        /// <summary>
        /// Gets the transactions following the one whose hash has been provided.
        /// </summary>
        /// <param name="from">The hash of the transaction to start streaming from.</param>
        /// <returns>An observable representing the transaction stream.</returns>
        IObservable<BinaryData> GetTransactionStream(BinaryData from);
    }
}
