using System;
using System.Linq;
using OpenChain.Core;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class BinaryValueTests
    {
        private readonly BinaryValueUsage usage = (BinaryValueUsage)10000;

        [Fact]
        public void Read_DefaultValue()
        {
            BinaryValue result = BinaryValue.Read(BinaryData.Empty, isKey: false);

            Assert.Equal(BinaryValueType.Default, result.Type);
            Assert.Equal(BinaryValueUsage.None, result.Usage);
            Assert.Equal(BinaryValueUsage.None, result.Usage);
            Assert.Equal(BinaryData.Empty, result.BinaryData);
        }

        [Fact]
        public void Read_AccountKey()
        {
            AccountKey accountKey = new AccountKey(usage, "/account/1", "/asset/1");

            AccountKey result = BinaryValue.Read(accountKey.BinaryData, isKey: true) as AccountKey;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueType.StringPair, result.Type);
            Assert.Equal(usage, result.Usage);
            Assert.Equal("/account/1", result.Account.FullPath);
            Assert.Equal("/asset/1", result.Asset.FullPath);
            Assert.Equal(accountKey.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_TextValue()
        {
            TextValue textValue = new TextValue(usage, "Text Value");

            TextValue result = BinaryValue.Read(textValue.BinaryData, isKey: true) as TextValue;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueType.String, result.Type);
            Assert.Equal(usage, result.Usage);
            Assert.Equal("Text Value", result.Value);
            Assert.Equal(textValue.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_Int64Value()
        {
            Int64Value intValue = new Int64Value(usage, long.MaxValue - int.MaxValue);

            Int64Value result = BinaryValue.Read(intValue.BinaryData, isKey: true) as Int64Value;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueType.Int64, result.Type);
            Assert.Equal(usage, result.Usage);
            Assert.Equal(long.MaxValue - int.MaxValue, result.Value);
            Assert.Equal(intValue.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_NotKey()
        {
            Int64Value intValue = new Int64Value(BinaryValueUsage.None, long.MaxValue - int.MaxValue);

            Int64Value result = BinaryValue.Read(intValue.BinaryData, isKey: false) as Int64Value;

            Assert.NotNull(result);
            Assert.Equal(BinaryValueType.Int64, result.Type);
            Assert.Equal(BinaryValueUsage.None, result.Usage);
            Assert.Equal(long.MaxValue - int.MaxValue, result.Value);
            Assert.Equal(intValue.BinaryData, result.BinaryData);
        }

        [Fact]
        public void Read_KeyAsNonKey()
        {
            TextValue value = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(value.BinaryData, isKey: false));
        }

        [Fact]
        public void Read_NonKeyAsKey()
        {
            TextValue value = new TextValue(BinaryValueUsage.None, "Text Value");
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(value.BinaryData, isKey: true));
        }

        [Fact]
        public void Read_RountripFailing()
        {
            TextValue value = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");
            byte[] invalidValue = value.BinaryData.ToByteArray();
            invalidValue[invalidValue.Length - 1] = 128;

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue), isKey: true));
        }

        [Fact]
        public void Read_BufferTooLong()
        {
            TextValue value = new TextValue(BinaryValueUsage.AssetDefinition, "Text Value");
            byte[] invalidValue = value.BinaryData.ToByteArray().Concat(new byte[] { 0 }).ToArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue), isKey: true));
        }

        [Fact]
        public void Read_BufferTooShort()
        {
            Int64Value value = new Int64Value(BinaryValueUsage.None, 1);
            byte[] invalidValue = value.BinaryData.ToByteArray().Take(value.BinaryData.Value.Count - 1).ToArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryValue.Read(new BinaryData(invalidValue), isKey: false));
        }

        [Fact]
        public void Equals_Sucess()
        {
            AccountKey key1 = new AccountKey(BinaryValueUsage.AssetDefinition, "/the/path", "/the/asset");
            AccountKey key2 = new AccountKey(BinaryValueUsage.AssetDefinition, "/the/path", "/the/asset/");
            AccountKey key3 = new AccountKey(BinaryValueUsage.AssetDefinition, "/the/path", "/the/asset");

            Assert.Equal(true, key1.Equals(key3));
            Assert.Equal(true, key1.Equals((object)key3));
            Assert.Equal(key3.GetHashCode(), key1.GetHashCode());
            Assert.Equal(key1, key3);

            Assert.Equal(false, key1.Equals(key2));
            Assert.Equal(false, key1.Equals((object)key2));
            Assert.Equal(key3.GetHashCode(), key1.GetHashCode());
            Assert.NotEqual(key1, key2);

            Assert.Equal(false, key1.Equals((AccountKey)null));
            Assert.Equal(false, key1.Equals((object)null));
        }
    }
}
