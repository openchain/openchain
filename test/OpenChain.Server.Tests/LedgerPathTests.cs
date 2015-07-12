using Xunit;

namespace OpenChain.Ledger.Tests
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
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

            // Unicode characters
            result = LedgerPath.TryParse("/abc%20c d/ef❤ ☀ ☆ ☂ ☻", out path);

            Assert.Equal(true, result);
            Assert.Equal("/abc%20c d/ef❤ ☀ ☆ ☂ ☻", path.FullPath);
            Assert.Equal(false, path.IsDirectory);
            Assert.Equal<string>(new[] { "abc%20c d", "ef❤ ☀ ☆ ☂ ☻" }, path.Segments);
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
            // Missing leading slash
            LedgerPath path;
            bool result = LedgerPath.TryParse(value, out path);

            Assert.Equal(false, result);
            Assert.Equal(null, path);
        }
    }
}
