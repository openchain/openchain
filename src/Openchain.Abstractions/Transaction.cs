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

namespace Openchain
{
    /// <summary>
    /// Represents a transaction affecting the data store.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="mutation">The binary representation of the <see cref="Mutation"/> applied by this transaction.</param>
        /// <param name="timestamp">The timestamp of the transaction.</param>
        /// <param name="transactionMetadata">The metadata associated with the transaction.</param>
        public Transaction(ByteString mutation, DateTime timestamp, ByteString transactionMetadata)
        {
            if (mutation == null)
                throw new ArgumentNullException(nameof(mutation));

            if (transactionMetadata == null)
                throw new ArgumentNullException(nameof(transactionMetadata));

            this.Mutation = mutation;
            this.Timestamp = timestamp;
            this.TransactionMetadata = transactionMetadata;
        }

        /// <summary>
        /// Gets the binary representation of the <see cref="Mutation"/> applied by this transaction.
        /// </summary>
        public ByteString Mutation { get; }

        /// <summary>
        /// Gets the timestamp of the transaction.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the metadata associated with the transaction.
        /// </summary>
        public ByteString TransactionMetadata { get; }
    }
}
