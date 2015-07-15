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
    }
}
