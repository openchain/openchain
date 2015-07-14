using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class LedgerPathTests
    {
        [Fact]
        public void TryParse_Success()
        {
            // Normal case
            LedgerPath path;
            bool result = LedgerPath.TryParse("/abc/def", out path);

            Assert.Equal(true, result);
            Assert.Equal("/abc/def", path.FullPath);
            Assert.Equal(false, path.IsDirectory);
            Assert.Equal<string>(new[] { "abc", "def" }, path.Segments);

            // Directory
            result = LedgerPath.TryParse("/abc/def/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/abc/def/", path.FullPath);
            Assert.Equal(true, path.IsDirectory);
            Assert.Equal<string>(new[] { "abc", "def" }, path.Segments);

            // Root
            result = LedgerPath.TryParse("/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/", path.FullPath);
            Assert.Equal(true, path.IsDirectory);
            Assert.Equal<string>(new string[] { }, path.Segments);
        }

        [Theory]
        // Missing leading slash
        [InlineData("abc/def")]
        [InlineData("abc")]
        // Null character
        [InlineData("/abc" + "\x000")]
        [InlineData("")]
        // Empty segment
        [InlineData("/abc//def")]
        [InlineData("/abc/def//")]
        public void TryParse_Invalid(string value)
        {
            LedgerPath path;
            bool result = LedgerPath.TryParse(value, out path);

            Assert.Equal(false, result);
            Assert.Equal(null, path);
        }

        [Fact]
        public void TryParse_InvalidCharacter()
        {
            const string invalidCharacters = " \"#%&/:;<=>?@[\\]^`{|}~\t\r\n\0é";

            foreach (char c in invalidCharacters)
            {
                LedgerPath path;
                bool result = LedgerPath.TryParse("/" + c, out path);

                Assert.Equal(null, path);
                Assert.Equal(false, result);
                Assert.Equal(false, LedgerPath.IsValidPathSegment(c.ToString()));
            }
        }
    }
}
