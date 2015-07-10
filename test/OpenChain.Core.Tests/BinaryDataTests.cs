using System;
using System.Collections;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class BinaryDataTests
    {
        [Fact]
        public void Constructor_Success()
        {
            byte[] sourceArray = new byte[] { 18, 178, 255, 70, 0 };
            BinaryData result = new BinaryData(sourceArray);
            sourceArray[4] = 1;

            Assert.NotSame(sourceArray, result.Value);
            Assert.Equal<byte>(new byte[] { 18, 178, 255, 70, 0 }, result.Value);
        }

        [Fact]
        public void Parse_Success()
        {
            BinaryData result = BinaryData.Parse("12b2FE460035789ACd");

            Assert.Equal<byte>(new byte[] { 18, 178, 254, 70, 0, 53, 120, 154, 205 }, result.Value);
        }

        [Fact]
        public void Parse_InvalidLength()
        {
            Assert.Throws<FormatException>(
                () => BinaryData.Parse("12b2ff460"));
        }

        [Fact]
        public void Parse_InvalidCharacter()
        {
            Assert.Throws<FormatException>(
                () => BinaryData.Parse("1G"));

            Assert.Throws<FormatException>(
                () => BinaryData.Parse("1/"));
        }

        [Fact]
        public void Parse_Null()
        {
            Assert.Throws<FormatException>(
                () => BinaryData.Parse(null));
        }

        [Fact]
        public void ToArray_Success()
        {
            byte[] sourceArray = new byte[] { 18, 178, 255, 70, 0 };
            BinaryData result = new BinaryData(sourceArray);

            Assert.Equal<byte>(new byte[] { 18, 178, 255, 70, 0 }, result.ToByteArray());
        }

        [Fact]
        public void ToString_Success()
        {
            string result = new BinaryData(new byte[] { 18, 178, 255, 70, 0 }).ToString();

            Assert.Equal("12b2ff4600", result);
        }
    }
}
