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

namespace Openchain
{
    /// <summary>
    /// Represents a data store for key-value pairs.
    /// </summary>
    public interface IStorageEngine : IDisposable
    {
        /// <summary>
        /// Initializes the storage engine.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task Initialize();

        /// <summary>
        /// Adds multiple transactions to the store.
        /// Either all transactions are committed or none are.
        /// </summary>
        /// <param name="transactions">A collection of serialized <see cref="Transaction"/> objects to add to the store.</param>
        /// <exception cref="ConcurrentMutationException">A record has been mutated and the transaction is no longer valid.</exception>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task AddTransactions(IEnumerable<ByteString> transactions);

        /// <summary>
        /// Gets the current records for a set of keys.
        /// </summary>
        /// <param name="keys">The keys to query.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<IReadOnlyList<Record>> GetRecords(IEnumerable<ByteString> keys);

        /// <summary>
        /// Gets the hash of the last transaction in the ledger.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<ByteString> GetLastTransaction();

        /// <summary>
        /// Gets an ordered list of transactions from a given point.
        /// </summary>
        /// <param name="from">The hash of the transaction to start from.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<IReadOnlyList<ByteString>> GetTransactions(ByteString from);
    }
}
