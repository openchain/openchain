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
                "/:ACC:/key/e/",
                "/:ACC:/key/",
                "/:ACC:/key./",
                "/:ACC:/key0/");

            IReadOnlyList<Record> result = await store.GetKeyStartingFrom(new ByteString(Encoding.UTF8.GetBytes("/:ACC:/key/")));

            Assert.Equal(2, result.Count);
            Assert.True(result.Any(record => Encoding.UTF8.GetString(record.Key.ToByteArray()) == "/:ACC:/key/e/"));
            Assert.True(result.Any(record => Encoding.UTF8.GetString(record.Key.ToByteArray()) == "/:ACC:/key/"));
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
