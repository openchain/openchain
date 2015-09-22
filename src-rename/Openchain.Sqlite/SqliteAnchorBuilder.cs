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
using OpenChain.Ledger;

namespace OpenChain.Sqlite
{
    public class SqliteAnchorBuilder : SqliteTransactionStore, IAnchorBuilder
    {
        public SqliteAnchorBuilder(string filename)
            : base(filename)
        {
        }

        public override async Task EnsureTables()
        {
            await base.EnsureTables();

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

        public async Task<LedgerAnchor> CreateAnchor()
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
                newTransactions = await ExecuteAsync(@"
                        SELECT  Hash
                        FROM    Transactions
                        WHERE   Id > (SELECT Id FROM Transactions WHERE Hash = @hash)
                        ORDER BY Id",
                    reader => new ByteString((byte[])reader.GetValue(0)),
                    new Dictionary<string, object>()
                    {
                        ["@hash"] = lastAnchor.Position.ToByteArray()
                    });

                currentHash = lastAnchor.FullStoreHash.ToByteArray();
            }
            else
            {
                newTransactions = await ExecuteAsync(@"
                        SELECT  Hash
                        FROM    Transactions
                        ORDER BY Id",
                    reader => new ByteString((byte[])reader.GetValue(0)),
                    new Dictionary<string, object>());

                currentHash = new byte[32];
            }

            if (newTransactions.Count == 0)
                return null;

            byte[] buffer = new byte[64];
            using (SHA256 sha = SHA256.Create())
            {
                foreach (ByteString transactionHash in newTransactions)
                {
                    currentHash.CopyTo(buffer, 0);
                    transactionHash.CopyTo(buffer, 32);

                    currentHash = sha.ComputeHash(sha.ComputeHash(buffer));
                }
            }

            LedgerAnchor result = new LedgerAnchor(
                newTransactions[newTransactions.Count - 1],
                new ByteString(currentHash),
                newTransactions.Count + (lastAnchor != null ? lastAnchor.TransactionCount : 0));

            await RecordAnchor(result);

            return result;
        }

        private async Task RecordAnchor(LedgerAnchor result)
        {
            await ExecuteAsync(@"
                    INSERT INTO Anchors
                    (Position, FullLedgerHash, TransactionCount)
                    VALUES (@position, @fullLedgerHash, @transactionCount)",
                new Dictionary<string, object>()
                {
                    ["@position"] = result.Position.ToByteArray(),
                    ["@fullLedgerHash"] = result.FullStoreHash.ToByteArray(),
                    ["@transactionCount"] = result.TransactionCount
                });
        }
    }
}
