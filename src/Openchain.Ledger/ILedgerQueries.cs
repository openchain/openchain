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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Openchain.Ledger
{
    /// <summary>
    /// Represents a set of query operations that can be performed against a transaction store.
    /// </summary>
    public interface ILedgerQueries
    {
        /// <summary>
        /// Returns all the record that have a key starting by the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix to query for.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix);

        /// <summary>
        /// Returns a transaction serialized as a <see cref="ByteString"/>, given its hash.
        /// </summary>
        /// <param name="mutationHash">The hash of the transaction.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<ByteString> GetTransaction(ByteString mutationHash);

        /// <summary>
        /// Returns a list of mutation hashes that have affected a given record.
        /// </summary>
        /// <param name="recordKey">The key of the record of which mutations are being retrieved.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey);
    }
}
