using System;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class MessageSerializerTests
    {
        [Fact]
        public void Transaction()
        {
            Transaction transaction = new Transaction(
                "ledgerId",
                new[]
                {
                    new AccountEntry(new AccountKey("account1", "asset1"), 100, BinaryData.Parse("1234")),
                    new AccountEntry(new AccountKey("account2", "asset2"), 200, BinaryData.Parse("5678")),
                },
                BinaryData.Parse("abcdef"));

            byte[] result = MessageSerializer.SerializeTransaction(transaction);

            Transaction finalTransaction = MessageSerializer.DeserializeTransaction(result);

            Assert.Equal(69, result.Length);
            Assert.Equal(transaction.AccountEntries.Count, finalTransaction.AccountEntries.Count);
            Assert.Equal(transaction.AccountEntries[0].AccountKey, finalTransaction.AccountEntries[0].AccountKey);
            Assert.Equal(transaction.AccountEntries[0].Amount, finalTransaction.AccountEntries[0].Amount);
            Assert.Equal(transaction.AccountEntries[0].Version, finalTransaction.AccountEntries[0].Version);
            Assert.Equal(transaction.AccountEntries[1].AccountKey, finalTransaction.AccountEntries[1].AccountKey);
            Assert.Equal(transaction.AccountEntries[1].Amount, finalTransaction.AccountEntries[1].Amount);
            Assert.Equal(transaction.AccountEntries[1].Version, finalTransaction.AccountEntries[1].Version);
            Assert.Equal(transaction.LedgerId, finalTransaction.LedgerId);
            Assert.Equal(transaction.Metadata, finalTransaction.Metadata);
        }

        [Fact]
        public void LedgerRecord()
        {
            LedgerRecord record = new LedgerRecord(
                BinaryData.Parse("abcd"),
                new DateTime(1, 2, 3, 4, 5, 6),
                BinaryData.Parse("01234567"));

            byte[] result = MessageSerializer.SerializeLedgerRecord(record);

            LedgerRecord finalRecord = MessageSerializer.DeserializeLedgerRecord(result);

            Assert.Equal(21, result.Length);
            Assert.Equal(record.Transaction, finalRecord.Transaction);
            Assert.Equal(record.Timestamp, finalRecord.Timestamp);
            Assert.Equal(record.ExternalMetadata, finalRecord.ExternalMetadata);
        }
    }
}