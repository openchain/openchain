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

namespace OpenChain.Ledger
{
    public class LedgerAnchor
    {
        public LedgerAnchor(ByteString position, ByteString fullStoreHash, long transactionCount)
        {
            Position = position;
            FullStoreHash = fullStoreHash;
            TransactionCount = transactionCount;
        }

        public ByteString Position { get; }

        public ByteString FullStoreHash { get; }

        public long TransactionCount { get; }
    }
}
