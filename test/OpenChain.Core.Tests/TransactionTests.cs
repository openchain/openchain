using System;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class TransactionTests
    {
        [Fact]
        public void Transaction_Success()
        {
            Transaction transaction = new Transaction(
                "ledgerId",
                new[]
                {
                    new AccountEntry(new AccountKey("account1", "asset1"), 100, BinaryData.Parse("1234")),
                    new AccountEntry(new AccountKey("account2", "asset2"), 200, BinaryData.Parse("5678")),
                },
                BinaryData.Parse("abcdef"));

            Assert.Equal(2, transaction.AccountEntries.Count);
            Assert.Equal("account1", transaction.AccountEntries[0].AccountKey.Account);
            Assert.Equal("asset1", transaction.AccountEntries[0].AccountKey.Asset);
            Assert.Equal(100, transaction.AccountEntries[0].Amount);
            Assert.Equal(BinaryData.Parse("1234"), transaction.AccountEntries[0].Version);
            Assert.Equal("ledgerId", transaction.LedgerId);
            Assert.Equal(BinaryData.Parse("abcdef"), transaction.Metadata);
        }

        [Fact]
        public void Transaction_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Transaction(
                null,
                new[] { new AccountEntry(new AccountKey("account1", "asset1"), 100, BinaryData.Parse("1234")) },
                BinaryData.Parse("abcdef")));

            Assert.Throws<ArgumentNullException>(() => new Transaction(
                "ledgerId",
                null,
                BinaryData.Parse("abcdef")));

            Assert.Throws<ArgumentNullException>(() => new Transaction(
                "ledgerId",
                new[] { new AccountEntry(new AccountKey("account1", "asset1"), 100, BinaryData.Parse("1234")) },
                null));

            Assert.Throws<ArgumentNullException>(() => new Transaction(
                "ledgerId",
                new[] { new AccountEntry(new AccountKey("account1", "asset1"), 100, BinaryData.Parse("1234")), null },
                BinaryData.Parse("abcdef")));

            Assert.Throws<ArgumentNullException>(() =>
                new AccountEntry(null, 100, BinaryData.Parse("1234")));

            Assert.Throws<ArgumentNullException>(() =>
                new AccountEntry(new AccountKey("account1", "asset1"), 100, null));

            Assert.Throws<ArgumentNullException>(() => new AccountKey(null, "asset1"));

            Assert.Throws<ArgumentNullException>(() => new AccountKey("account1", null));
        }
    }
}
