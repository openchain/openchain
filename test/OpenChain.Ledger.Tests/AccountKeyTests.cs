using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class AccountKeyTests
    {
        [Fact]
        public void Equals_Success()
        {
            Assert.True(AccountKey.Parse("/abc/", "/def/").Equals(AccountKey.Parse("/abc/", "/def/")));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(AccountKey.Parse("/abc/", "/ghi/")));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(null));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(100));
        }

        [Fact]
        public void GetHashCode_Success()
        {
            Assert.Equal(AccountKey.Parse("/abc/", "/def/").GetHashCode(), AccountKey.Parse("/abc/", "/def/").GetHashCode());
        }
    }
}
