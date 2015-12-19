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

namespace Openchain.Infrastructure
{
    /// <summary>
    /// Represents a database anchor containing the cumulative hash of the data store.
    /// </summary>
    public class LedgerAnchor
    {
        public LedgerAnchor(ByteString position, ByteString fullStoreHash, long transactionCount)
        {
            Position = position;
            FullStoreHash = fullStoreHash;
            TransactionCount = transactionCount;
        }

        /// <summary>
        /// Gets the hash of the last transaction in the ledger in the current state.
        /// </summary>
        public ByteString Position { get; }

        /// <summary>
        /// Gets the cumulative hash of the ledger.
        /// </summary>
        public ByteString FullStoreHash { get; }

        /// <summary>
        /// Gets the total count of transactions in the ledger.
        /// </summary>
        public long TransactionCount { get; }
    }
}
