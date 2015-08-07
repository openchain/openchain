using System;
using Xunit;

namespace OpenChain.Tests
{
    public class ByteStringTests
    {
        [Fact]
        public void Constructor_Success()
        {
            byte[] sourceArray = new byte[] { 18, 178, 255, 70, 0 };
            ByteString result = new ByteString(sourceArray);
            sourceArray[4] = 1;

            Assert.NotSame(sourceArray, result.Value);
            Assert.Equal<byte>(new byte[] { 18, 178, 255, 70, 0 }, result.Value);
        }

        [Fact]
        public void Parse_Success()
        {
            ByteString result = ByteString.Parse("12b2FE460035789ACd");

            Assert.Equal<byte>(new byte[] { 18, 178, 254, 70, 0, 53, 120, 154, 205 }, result.Value);
        }

        [Fact]
        public void Parse_InvalidLength()
        {
            Assert.Throws<FormatException>(
                () => ByteString.Parse("12b2ff460"));
        }

        [Fact]
        public void Parse_InvalidCharacter()
        {
            Assert.Throws<FormatException>(
                () => ByteString.Parse("1G"));

            Assert.Throws<FormatException>(
                () => ByteString.Parse("1/"));
        }

        [Fact]
        public void Parse_Null()
        {
            Assert.Throws<FormatException>(
                () => ByteString.Parse(null));
        }

        [Fact]
        public void ToArray_Success()
        {
            byte[] sourceArray = new byte[] { 18, 178, 255, 70, 0 };
            ByteString result = new ByteString(sourceArray);

            Assert.Equal<byte>(new byte[] { 18, 178, 255, 70, 0 }, result.ToByteArray());
        }

        [Fact]
        public void ToString_Success()
        {
            string result = new ByteString(new byte[] { 18, 178, 255, 70, 0 }).ToString();

            Assert.Equal("12b2ff4600", result);
        }
    }
}
