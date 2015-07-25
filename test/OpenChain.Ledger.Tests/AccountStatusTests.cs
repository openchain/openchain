using System.Linq;
using Assert = Xunit.Assert;
using Fact = Xunit.FactAttribute;

namespace OpenChain.Ledger.Tests
{
    public class AccountStatusTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void FromRecord_Set()
        {
            Record pair = new Record(
                new AccountKey(BinaryValueUsage.Account, "/the/account", "/the/asset").BinaryData,
                new Int64Value(BinaryValueUsage.None, 100).BinaryData,
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(pair);

            Assert.Equal("/the/account", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset", status.AccountKey.Asset.FullPath);
            Assert.Equal(100, status.Balance);
            Assert.Equal(binaryData[1], status.Version);
        }

        [Fact]
        public void FromRecord_Unset()
        {
            Record pair = new Record(
                new AccountKey(BinaryValueUsage.Account, "/the/account", "/the/asset").BinaryData,
                BinaryData.Empty,
                BinaryData.Empty);

            AccountStatus status = AccountStatus.FromRecord(pair);

            Assert.Equal("/the/account", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset", status.AccountKey.Asset.FullPath);
            Assert.Equal(0, status.Balance);
            Assert.Equal(BinaryData.Empty, status.Version);
        }

        [Fact]
        public void FromRecord_InvalidKey()
        {
            Record pair = new Record(
                new TextValue(BinaryValueUsage.Alias, "Text Value").BinaryData,
                new Int64Value(BinaryValueUsage.None, 100).BinaryData,
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(pair);

            Assert.Null(status);
        }

        [Fact]
        public void FromRecord_InvalidBinaryData()
        {
            Record pair = new Record(
                new TextValue(BinaryValueUsage.Account, "Text Value").BinaryData,
                new Int64Value(BinaryValueUsage.None, 100).BinaryData,
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(pair);

            Assert.Null(status);
        }
    }
}
