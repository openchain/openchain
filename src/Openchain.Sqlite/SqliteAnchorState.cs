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
using System.Linq;
using System.Threading.Tasks;
using Openchain.Ledger;

namespace Openchain.Sqlite
{
    /// <summary>
    /// Persists information about the latest known anchor.
    /// </summary>
    public class SqliteAnchorState : SqliteBase, IAnchorState
    {
        public SqliteAnchorState(string filename)
            : base(filename)
        { }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task Initialize()
        {
            await Connection.OpenAsync();
        }

        /// <summary>
        /// Gets the last known anchor.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<LedgerAnchor> GetLastAnchor()
        {
            IEnumerable<LedgerAnchor> anchors = await ExecuteAsync(@"
                    SELECT  Position, FullLedgerHash, TransactionCount
                    FROM    Anchors
                    ORDER BY Id DESC
                    LIMIT 1",
                reader => new LedgerAnchor(
                    new ByteString((byte[])reader.GetValue(0)),
                    new ByteString((byte[])reader.GetValue(1)),
                    reader.GetInt64(2)),
                new Dictionary<string, object>());

            return anchors.FirstOrDefault();
        }

        /// <summary>
        /// Marks the anchor as successfully recorded in the anchoring medium.
        /// </summary>
        /// <param name="anchor">The anchor to commit.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task CommitAnchor(LedgerAnchor anchor)
        {
            await ExecuteAsync(@"
                    INSERT INTO Anchors
                    (Position, FullLedgerHash, TransactionCount)
                    VALUES (@position, @fullLedgerHash, @transactionCount)",
                new Dictionary<string, object>()
                {
                    ["@position"] = anchor.Position.ToByteArray(),
                    ["@fullLedgerHash"] = anchor.FullStoreHash.ToByteArray(),
                    ["@transactionCount"] = anchor.TransactionCount
                });
        }
    }
}
