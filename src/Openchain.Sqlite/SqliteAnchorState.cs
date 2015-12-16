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
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Openchain.Ledger;

namespace Openchain.Sqlite
{
    /// <summary>
    /// Persists information about the latest known anchor.
    /// </summary>
    public class SqliteAnchorState : SqliteBase, IAnchorBuilder
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

            SqliteCommand command = Connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Anchors
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Position BLOB UNIQUE,
                    FullLedgerHash BLOB,
                    TransactionCount INT,
                    AnchorId BLOB
                );
            ";

            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Creates a database anchor for the current state of the database.
        /// </summary>
        /// <param name="storageEngine">The source of transactions to use.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<LedgerAnchor> CreateAnchor(IStorageEngine storage)
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

            LedgerAnchor lastAnchor = anchors.FirstOrDefault();

            IReadOnlyList<ByteString> newTransactions;
            byte[] currentHash;
            if (lastAnchor != null)
            {
                newTransactions = await storage.GetTransactions(lastAnchor.Position);
                currentHash = lastAnchor.FullStoreHash.ToByteArray();
            }
            else
            {
                newTransactions = await storage.GetTransactions(null);
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
