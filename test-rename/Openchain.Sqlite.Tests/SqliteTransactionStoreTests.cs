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
using System.Threading.Tasks;
using Xunit;

namespace OpenChain.Sqlite.Tests
{
    public class SqliteTransactionStoreTests
    {
        private readonly SqliteTransactionStore store;
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        public SqliteTransactionStoreTests()
        {
            this.store = new SqliteTransactionStore(":memory:");
            this.store.EnsureTables().Wait();
        }

        [Fact]
        public async Task AddTransaction_InsertSuccess()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[2], null, ByteString.Empty),
                new Record(binaryData[4], ByteString.Empty, ByteString.Empty));

            IList<Record> records1 = await this.store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.store.GetRecords(new[] { binaryData[2] });
            IList<Record> records3 = await this.store.GetRecords(new[] { binaryData[4] });
            IList<Record> records4 = await this.store.GetRecords(new[] { binaryData[6] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[2], ByteString.Empty, ByteString.Empty);
            Assert.Equal(1, records3.Count);
            AssertRecord(records3[0], binaryData[4], ByteString.Empty, mutationHash);
            Assert.Equal(1, records4.Count);
            AssertRecord(records4[0], binaryData[6], ByteString.Empty, ByteString.Empty);
        }

        [Fact]
        public async Task AddTransaction_UpdateSuccess()
        {
            ByteString mutationHash1 = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty));

            ByteString mutationHash2 = await AddTransaction(
                new Record(binaryData[3], binaryData[4], ByteString.Empty));

            ByteString mutationHash3 = await AddTransaction(
                new Record(binaryData[0], binaryData[2], mutationHash1),
                new Record(binaryData[3], null, mutationHash2));

            IList<Record> records1 = await this.store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.store.GetRecords(new[] { binaryData[3] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[2], mutationHash3);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[3], binaryData[4], mutationHash2);
        }

        [Fact]
        public async Task AddTransaction_InsertError()
        {
            await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[0], binaryData[1], binaryData[2])));

            await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[3], null, binaryData[4])));

            IList<Record> records1 = await this.store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.store.GetRecords(new[] { binaryData[3] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], ByteString.Empty, ByteString.Empty);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[3], ByteString.Empty, ByteString.Empty);
        }

        [Fact]
        public async Task AddTransaction_UpdateError()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[4], binaryData[5], ByteString.Empty));

            await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[0], binaryData[2], binaryData[3])));

            await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[4], null, binaryData[6])));

            IList<Record> records1 = await this.store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.store.GetRecords(new[] { binaryData[4] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[4], binaryData[5], mutationHash);
        }

        private async Task<ByteString> AddTransaction(params Record[] records)
        {
            Mutation mutation = new Mutation(ByteString.Parse("0123"), records, ByteString.Parse("4567"));
            ByteString serializedMutation = new ByteString(MessageSerializer.SerializeMutation(mutation));
            Transaction transaction = new Transaction(
                serializedMutation,
                new DateTime(1, 2, 3, 4, 5, 6),
                ByteString.Parse("abcdef"));

            await this.store.AddTransactions(new[] { new ByteString(MessageSerializer.SerializeTransaction(transaction)) });

            return new ByteString(MessageSerializer.ComputeHash(serializedMutation.ToByteArray()));
        }

        private static void AssertRecord(Record record, ByteString key, ByteString value, ByteString version)
        {
            Assert.Equal(key, record.Key);
            Assert.Equal(value, record.Value);
            Assert.Equal(version, record.Version);
        }
    }
}
