using System;
using System.Linq;
using Xunit;

namespace OpenChain.Tests
{
    public class TransactionTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Transaction_Success()
        {
            Transaction record = new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]);

            Assert.Equal(binaryData[0], record.Mutation);
            Assert.Equal(new DateTime(1, 2, 3, 4, 5, 6), record.Timestamp);
            Assert.Equal(binaryData[1], record.TransactionMetadata);
        }

        [Fact]
        public void Transaction_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Transaction(
                null,
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]));

            Assert.Throws<ArgumentNullException>(() => new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                null));
        }
    }
}
