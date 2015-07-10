using System;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class LedgerRecordTests
    {
        [Fact]
        public void LedgerRecord_Success()
        {
            LedgerRecord record = new LedgerRecord(
                BinaryData.Parse("123456"),
                new DateTime(1, 2, 3, 4, 5, 6),
                BinaryData.Parse("abcdef"));

            Assert.Equal(BinaryData.Parse("123456"), record.Transaction);
            Assert.Equal(new DateTime(1, 2, 3, 4, 5, 6), record.Timestamp);
            Assert.Equal(BinaryData.Parse("abcdef"), record.ExternalMetadata);
        }

        [Fact]
        public void LedgerRecord_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LedgerRecord(
                null,
                new DateTime(1, 2, 3, 4, 5, 6),
                BinaryData.Parse("abcdef")));

            Assert.Throws<ArgumentNullException>(() => new LedgerRecord(
                BinaryData.Parse("123456"),
                new DateTime(1, 2, 3, 4, 5, 6),
                null));
        }
    }
}
