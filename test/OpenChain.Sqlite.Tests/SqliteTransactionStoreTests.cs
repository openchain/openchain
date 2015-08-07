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
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        public SqliteTransactionStoreTests()
        {
            this.store = new SqliteTransactionStore(":memory:");
            this.store.EnsureTables().Wait();
        }

        [Fact]
        public async Task AddTransaction_InsertSuccess()
        {
            BinaryData mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], BinaryData.Empty),
                new Record(binaryData[2], null, BinaryData.Empty),
                new Record(binaryData[4], BinaryData.Empty, BinaryData.Empty));

            IList<Record> records1 = await this.store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.store.GetRecords(new[] { binaryData[2] });
            IList<Record> records3 = await this.store.GetRecords(new[] { binaryData[4] });
            IList<Record> records4 = await this.store.GetRecords(new[] { binaryData[6] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[2], BinaryData.Empty, BinaryData.Empty);
            Assert.Equal(1, records3.Count);
            AssertRecord(records3[0], binaryData[4], BinaryData.Empty, mutationHash);
            Assert.Equal(1, records4.Count);
            AssertRecord(records4[0], binaryData[6], BinaryData.Empty, BinaryData.Empty);
        }

        [Fact]
        public async Task AddTransaction_UpdateSuccess()
        {
            BinaryData mutationHash1 = await AddTransaction(
                new Record(binaryData[0], binaryData[1], BinaryData.Empty));

            BinaryData mutationHash2 = await AddTransaction(
                new Record(binaryData[3], binaryData[4], BinaryData.Empty));

            BinaryData mutationHash3 = await AddTransaction(
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
            AssertRecord(records1[0], binaryData[0], BinaryData.Empty, BinaryData.Empty);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[3], BinaryData.Empty, BinaryData.Empty);
        }

        [Fact]
        public async Task AddTransaction_UpdateError()
        {
            BinaryData mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], BinaryData.Empty),
                new Record(binaryData[4], binaryData[5], BinaryData.Empty));

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

        private async Task<BinaryData> AddTransaction(params Record[] records)
        {
            Mutation mutation = new Mutation(BinaryData.Parse("0123"), records, BinaryData.Parse("4567"));
            BinaryData serializedMutation = new BinaryData(MessageSerializer.SerializeMutation(mutation));
            Transaction transaction = new Transaction(
                serializedMutation,
                new DateTime(1, 2, 3, 4, 5, 6),
                BinaryData.Parse("abcdef"));

            await this.store.AddTransactions(new[] { new BinaryData(MessageSerializer.SerializeTransaction(transaction)) });

            return new BinaryData(MessageSerializer.ComputeHash(serializedMutation.ToByteArray()));
        }

        private static void AssertRecord(Record record, BinaryData key, BinaryData value, BinaryData version)
        {
            Assert.Equal(key, record.Key);
            Assert.Equal(value, record.Value);
            Assert.Equal(version, record.Version);
        }
    }
}
