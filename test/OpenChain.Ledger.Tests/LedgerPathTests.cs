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
            bool result = LedgerPath.TryParse("/abc/def/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/abc/def/", path.FullPath);
            Assert.Equal<string>(new[] { "abc", "def" }, path.Segments);

            // All characters
            result = LedgerPath.TryParse("/azAZ0189$-_.+!*'(),/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/azAZ0189$-_.+!*'(),/", path.FullPath);
            Assert.Equal<string>(new[] { "azAZ0189$-_.+!*'()," }, path.Segments);

            // Directory
            result = LedgerPath.TryParse("/abc/def/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/abc/def/", path.FullPath);
            Assert.Equal<string>(new[] { "abc", "def" }, path.Segments);

            // Root
            result = LedgerPath.TryParse("/", out path);

            Assert.Equal(true, result);
            Assert.Equal("/", path.FullPath);
            Assert.Equal<string>(new string[] { }, path.Segments);
        }

        [Theory]
        // Missing leading slash
        [InlineData("abc/def/")]
        [InlineData("abc/")]
        // Missing final slash
        [InlineData("/abc/def")]
        [InlineData("/abc")]
        [InlineData("")]
        // Null character
        [InlineData("/abc" + "\x000" + "/")]
        // Empty segment
        [InlineData("/abc//def/")]
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
                bool result = LedgerPath.TryParse("/" + c + "/", out path);

                Assert.Equal(null, path);
                Assert.Equal(false, result);
                Assert.Equal(false, LedgerPath.IsValidPathSegment(c.ToString()));
            }
        }

        [Fact]
        public void FromSegments_Success()
        {
            LedgerPath path = LedgerPath.FromSegments();
            Assert.Equal("/", path.FullPath);
            Assert.Equal<string>(new string[0], path.Segments);

            path = LedgerPath.FromSegments("a");
            Assert.Equal("/a/", path.FullPath);
            Assert.Equal<string>(new[] { "a" }, path.Segments);

            path = LedgerPath.FromSegments("a", "b");
            Assert.Equal("/a/b/", path.FullPath);
            Assert.Equal<string>(new[] { "a", "b" }, path.Segments);
        }

        [Fact]
        public void IsStrictParentOf_Success()
        {
            LedgerPath parent = LedgerPath.Parse("/the/parent/");

            Assert.True(parent.IsStrictParentOf(LedgerPath.Parse("/the/parent/child/")));
            Assert.True(parent.IsStrictParentOf(LedgerPath.Parse("/the/parent/child/child/")));
            Assert.False(parent.IsStrictParentOf(LedgerPath.Parse("/the/parent/")));
            Assert.False(parent.IsStrictParentOf(LedgerPath.Parse("/the/")));
            Assert.False(parent.IsStrictParentOf(LedgerPath.Parse("/not/related/")));
        }

        [Fact]
        public void IsParentOf_Success()
        {
            LedgerPath parent = LedgerPath.Parse("/the/parent/");

            Assert.True(parent.IsParentOf(LedgerPath.Parse("/the/parent/child/")));
            Assert.True(parent.IsParentOf(LedgerPath.Parse("/the/parent/child/child/")));
            Assert.True(parent.IsParentOf(LedgerPath.Parse("/the/parent/")));
            Assert.False(parent.IsParentOf(LedgerPath.Parse("/the/")));
            Assert.False(parent.IsParentOf(LedgerPath.Parse("/not/related/")));
        }
    }
}
