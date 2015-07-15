using System;
using System.Linq;
using OpenChain.Core;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class BinaryValueTests
    {
        [Fact]
        public void Read_AccountKey()
        {
            AccountKey accountKey = new AccountKey("/account/1", "/asset/1");

            AccountKey result = BinaryValue.Read(accountKey.BinaryData) as AccountKey;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueUsage.AccountKey, result.Usage);
            Assert.Equal("/account/1", result.Account.FullPath);
            Assert.Equal("/asset/1", result.Asset.FullPath);
            Assert.Equal(accountKey.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_TextValue()
        {
            TextValue textValue = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");

            TextValue result = BinaryValue.Read(textValue.BinaryData) as TextValue;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueUsage.AssetDefinition, result.Usage);
            Assert.Equal("Text Value", result.Value);
            Assert.Equal(textValue.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_Int64Value()
        {
            Int64Value intValue = new Int64Value(long.MaxValue - int.MaxValue);

            Int64Value result = BinaryValue.Read(intValue.BinaryData) as Int64Value;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueUsage.Int64, result.Usage);
            Assert.Equal(long.MaxValue - int.MaxValue, result.Value);
            Assert.Equal(intValue.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_InvalidUsage()
        {
            TextValue value = new TextValue((BinaryValueUsage)10000, "Text Value");

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(value.BinaryData));
        }

        [Fact]
        public void Read_RountripFailing()
        {
            TextValue value = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");
            byte[] invalidValue = value.BinaryData.ToByteArray();
            invalidValue[invalidValue.Length - 1] = 128;

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue)));
        }

        [Fact]
        public void Read_BufferTooLong()
        {
            TextValue value = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");
            byte[] invalidValue = value.BinaryData.ToByteArray().Concat(new byte[] { 0 }).ToArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue)));
        }

        [Fact]
        public void Read_BufferTooShort()
        {
            Int64Value value = new Int64Value(1);
            byte[] invalidValue = value.BinaryData.ToByteArray().Take(value.BinaryData.Value.Count - 1).ToArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue)));
        }
    }
}
