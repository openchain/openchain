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
using Microsoft.Data.Sqlite;
using Openchain.Ledger;

namespace Openchain.Sqlite
{
    public class SqliteLedgerQueries : SqliteTransactionStore, ILedgerQueries
    {
        private readonly string columnAlreadyExistsMessage = "SQLite Error 1: 'duplicate column name: Name'";

        public SqliteLedgerQueries(string filename)
            : base(filename)
        {
        }

        public override async Task EnsureTables()
        {
            await base.EnsureTables();

            try
            {
                await ExecuteAsync(
                    @"
                    ALTER TABLE Records ADD COLUMN Name TEXT;
                    ALTER TABLE Records ADD COLUMN Type INTEGER;",
                    new Dictionary<string, object>());
            }
            catch (SqliteException exception) when (exception.Message == columnAlreadyExistsMessage)
            { }
        }

        public async Task<ByteString> GetTransaction(ByteString mutationHash)
        {
            IEnumerable<ByteString> transactions = await ExecuteAsync(@"
                    SELECT  RawData
                    FROM    Transactions
                    WHERE   MutationHash = @mutationHash",
               reader => new ByteString((byte[])reader.GetValue(0)),
               new Dictionary<string, object>()
               {
                   ["@mutationHash"] = mutationHash.ToByteArray()
               });

            return transactions.FirstOrDefault();
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
        {
            byte[] from = prefix.ToByteArray();
            byte[] to = prefix.ToByteArray();

            if (to[to.Length - 1] < 255)
                to[to.Length - 1] += 1;

            return await ExecuteAsync(@"
                    SELECT  Key, Value, Version
                    FROM    Records
                    WHERE   Key >= @from AND Key < @to",
            reader => new Record(
                    new ByteString((byte[])reader.GetValue(0)),
                    reader.GetValue(1) == null ? ByteString.Empty : new ByteString((byte[])reader.GetValue(1)),
                    new ByteString((byte[])reader.GetValue(2))),
                new Dictionary<string, object>()
                {
                    ["@from"] = from,
                    ["@to"] = to
                });
        }

        protected override async Task AddTransaction(Mutation mutation)
        {
            foreach (Record record in mutation.Records)
            {
                RecordKey key = RecordKey.Parse(record.Key);
                    await ExecuteAsync(@"
                        UPDATE  Records
                        SET     Type = @type,
                                Name = @name
                        WHERE   Key = @key",
                    new Dictionary<string, object>()
                    {
                        ["@key"] = record.Key.ToByteArray(),
                        ["@type"] = (int)key.RecordType,
                        ["@name"] = key.Name
                    });
            }
        }
    }
}
