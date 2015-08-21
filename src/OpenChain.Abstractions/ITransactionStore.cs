// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain
{
    /// <summary>
    /// Represents a data store for key-value pairs.
    /// </summary>
    public interface ITransactionStore
    {
        /// <summary>
        /// Adds a transaction to the store.
        /// </summary>
        /// <param name="transactions">A collection of serialized <see cref="Transaction"/> to add to the store.</param>
        /// <exception cref="ConcurrentMutationException">A record has been mutated and the transaction is no longer valid.</exception>
        /// <returns>A task that represents the completion of the operation.</returns>
        Task AddTransactions(IEnumerable<ByteString> transactions);

        /// <summary>
        /// Gets the current records for a set of keys.
        /// </summary>
        /// <param name="keys">The keys to query.</param>
        /// <returns>A task that represents the completion of the operation and contains a list of the corresponding <see cref="Record"/>.</returns>
        Task<IList<Record>> GetRecords(IEnumerable<ByteString> keys);

        /// <summary>
        /// Gets the hash of the last transaction in the ledger.
        /// </summary>
        /// <returns>A task that represents the completion of the operation and contains the hash of the last transaction.</returns>
        Task<ByteString> GetLastTransaction();

        /// <summary>
        /// Gets the transactions following the one whose hash has been provided.
        /// </summary>
        /// <param name="from">The hash of the transaction to start streaming from.</param>
        /// <returns>An observable representing the transaction stream.</returns>
        IObservable<ByteString> GetTransactionStream(ByteString from);
    }
}
