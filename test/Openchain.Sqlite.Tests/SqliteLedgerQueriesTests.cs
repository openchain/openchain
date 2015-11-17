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

namespace Openchain.Sqlite.Tests
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
            await AddRecords("/:DATA:/name");

            IList<Record> result = await store.GetRecords(new[] { new ByteString(Encoding.UTF8.GetBytes("/:DATA:/name")) });

            Assert.Equal(1, result.Count);
            Assert.Equal("/:DATA:/name", Encoding.UTF8.GetString(result[0].Key.ToByteArray()));
            Assert.Equal(ByteString.Empty, result[0].Value);
        }

        [Fact]
        public async Task GetKeyStartingFrom_Success()
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

        [Fact]
        public async Task GetKeyStartingFrom_NonEmptyValues()
        {
            await AddRecords(ByteString.Empty, binaryData[0], "/:DATA:/name");

            IReadOnlyList<Record> result = await store.GetKeyStartingFrom(new ByteString(Encoding.UTF8.GetBytes("/:DATA:/")));

            Assert.Equal(1, result.Count);
            Assert.Equal("/:DATA:/name", Encoding.UTF8.GetString(result[0].Key.ToByteArray()));
            Assert.Equal(binaryData[0], result[0].Value);
        }

        [Fact]
        public async Task GetRecordMutations_Success()
        {
            ByteString mutation1 = await AddRecords("/:DATA:name");
            await AddRecords("/:DATA:other");
            ByteString mutation2 = await AddRecords(mutation1, ByteString.Empty, "/:DATA:name");

            IReadOnlyList<ByteString> result = await store.GetRecordMutations(new ByteString(Encoding.UTF8.GetBytes("/:DATA:name")));

            Assert.Equal<ByteString>(new[] { mutation1, mutation2 }, result);
        }

        [Fact]
        public async Task GetTransaction_Success()
        {
            await AddRecords("/:DATA:name1");
            ByteString mutation = await AddRecords("/:DATA:name2");
            await AddRecords("/:DATA:name3");

            ByteString result = await store.GetTransaction(mutation);

            Assert.Equal(mutation, new ByteString(MessageSerializer.ComputeHash(MessageSerializer.DeserializeTransaction(result).Mutation.ToByteArray())));
        }

        private Task<ByteString> AddRecords(params string[] keys)
        {
            return AddRecords(ByteString.Empty, ByteString.Empty, keys);
        }

        private async Task<ByteString> AddRecords(ByteString version, ByteString value, params string[] keys)
        {
            Mutation mutation = new Mutation(
                ByteString.Empty,
                keys.Select(key => new Record(
                    new ByteString(Encoding.UTF8.GetBytes(key)),
                    value,
                    version)),
                ByteString.Empty);

            byte[] serializedMutation = MessageSerializer.SerializeMutation(mutation);

            Transaction transaction = new Transaction(
                new ByteString(MessageSerializer.SerializeMutation(mutation)),
                new DateTime(),
                ByteString.Empty);

            await store.AddTransactions(new[] { new ByteString(MessageSerializer.SerializeTransaction(transaction)) });

            return new ByteString(MessageSerializer.ComputeHash(serializedMutation));
        }
    }
}
