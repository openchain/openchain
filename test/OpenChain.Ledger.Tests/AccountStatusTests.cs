using System;
using System.Linq;
using Assert = Xunit.Assert;
using Fact = Xunit.FactAttribute;

namespace OpenChain.Ledger.Tests
{
    public class AccountStatusTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void FromRecord_Set()
        {
            Record record = new Record(
                AccountKey.Parse("/the/account/", "/the/asset/").Key.ToBinary(),
                SerializeInt(100),
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(RecordKey.Parse(record.Key), record);

            Assert.Equal("/the/account/", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset/", status.AccountKey.Asset.FullPath);
            Assert.Equal(100, status.Balance);
            Assert.Equal(binaryData[1], status.Version);
        }

        [Fact]
        public void FromRecord_Unset()
        {
            Record record = new Record(
                AccountKey.Parse("/the/account/", "/the/asset/").Key.ToBinary(),
                ByteString.Empty,
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(RecordKey.Parse(record.Key), record);

            Assert.Equal("/the/account/", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset/", status.AccountKey.Asset.FullPath);
            Assert.Equal(0, status.Balance);
            Assert.Equal(binaryData[1], status.Version);
        }

        private static ByteString SerializeInt(long value)
        {
            return new ByteString(BitConverter.GetBytes(value).Reverse());
        }
    }
}
