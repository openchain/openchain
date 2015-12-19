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
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Openchain.Infrastructure
{
    /// <summary>
    /// Handles the calculation of ledger anchors and records them on the target medium.
    /// </summary>
    public class AnchorBuilder
    {
        private readonly IStorageEngine storageEngine;
        private readonly IAnchorRecorder anchorRecorder;
        private readonly IAnchorState anchorState;

        public AnchorBuilder(IStorageEngine storageEngine, IAnchorRecorder anchorRecorder, IAnchorState anchorState)
        {
            this.storageEngine = storageEngine;
            this.anchorRecorder = anchorRecorder;
            this.anchorState = anchorState;
        }

        /// <summary>
        /// Determines whether a new anchor should be calculated and recorded,
        /// and does it if it is the case.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<LedgerAnchor> RecordAnchor()
        {
            if (await anchorRecorder.CanRecordAnchor())
            {
                LedgerAnchor anchor = await anchorState.GetLastAnchor();
                LedgerAnchor latestAnchor = await ComputeNewAnchor(anchor);

                if (latestAnchor != null)
                {
                    // Record the anchor
                    await anchorRecorder.RecordAnchor(latestAnchor);

                    // Commit the anchor if it has been recorded successfully
                    await anchorState.CommitAnchor(latestAnchor);

                    return latestAnchor;
                }
            }

            return null;
        }

        private async Task<LedgerAnchor> ComputeNewAnchor(LedgerAnchor lastAnchor)
        {
            IReadOnlyList<ByteString> newTransactions;
            byte[] currentHash;
            if (lastAnchor != null)
            {
                newTransactions = await storageEngine.GetTransactions(lastAnchor.Position);
                currentHash = lastAnchor.FullStoreHash.ToByteArray();
            }
            else
            {
                newTransactions = await storageEngine.GetTransactions(null);
                currentHash = new byte[32];
            }

            if (newTransactions.Count == 0)
                return null;

            byte[] position = currentHash;
            byte[] buffer = new byte[64];
            using (SHA256 sha = SHA256.Create())
            {
                foreach (ByteString rawTransaction in newTransactions)
                {
                    currentHash.CopyTo(buffer, 0);
                    position = MessageSerializer.ComputeHash(rawTransaction.ToByteArray());
                    position.CopyTo(buffer, 32);

                    currentHash = sha.ComputeHash(sha.ComputeHash(buffer));
                }
            }

            LedgerAnchor result = new LedgerAnchor(
                new ByteString(position),
                new ByteString(currentHash),
                newTransactions.Count + (lastAnchor != null ? lastAnchor.TransactionCount : 0));

            return result;
        }
    }
}
