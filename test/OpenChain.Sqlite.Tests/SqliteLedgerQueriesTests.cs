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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OpenChain.Sqlite.Tests
{
    public class SqliteLedgerQueriesTests
    {
        private readonly SqliteLedgerQueries store;
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        public SqliteLedgerQueriesTests()
        {
            this.store = new SqliteLedgerQueries(":memory:");
            this.store.EnsureTables().Wait();
        }

        [Fact]
        public async Task AddTransaction_InsertSuccess()
        {
            await AddRecords(
                "/:DATA:/e",
                "/:DATA:/",
                "/:DATA:.",
                "/:DATA:0");

            IReadOnlyList<Record> result = await store.GetKeyStartingFrom(new ByteString(Encoding.UTF8.GetBytes("/:DATA:/")));

            Assert.Equal(2, result.Count);
            Assert.True(result.Any(record => Encoding.UTF8.GetString(record.Key.ToByteArray()) == "/:DATA:/e"));
            Assert.True(result.Any(record => Encoding.UTF8.GetString(record.Key.ToByteArray()) == "/:DATA:/"));
        }

        private async Task AddRecords(params string[] keys)
        {
            Mutation mutation = new Mutation(
                ByteString.Empty,
                keys.Select(key => new Record(
                    new ByteString(Encoding.UTF8.GetBytes(key)),
                    ByteString.Empty,
                    ByteString.Empty)),
                ByteString.Empty);

            Transaction transaction = new Transaction(
                new ByteString(MessageSerializer.SerializeMutation(mutation)),
                new DateTime(),
                ByteString.Empty);

            await store.AddTransactions(new[] { new ByteString(MessageSerializer.SerializeTransaction(transaction)) });
        }
    }
}
