using System.Linq;
using System.Text;
using OpenChain.Core;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class ParsedMutationTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Parse_AccountMutations()
        {
            ParsedMutation result = Parse(new KeyValuePair(
                new AccountKey("/the/account", "/the/asset").BinaryData,
                new Int64Value(100).BinaryData,
                binaryData[3]));

            Assert.Equal(1, result.AccountMutations.Count);
            Assert.Equal(0, result.AssetDefinitions.Count);
            Assert.Equal(0, result.Aliases.Count);
            Assert.Equal("/the/account", result.AccountMutations[0].AccountKey.Account.FullPath);
            Assert.Equal("/the/asset", result.AccountMutations[0].AccountKey.Asset.FullPath);
            Assert.Equal(100, result.AccountMutations[0].Balance);
            Assert.Equal(binaryData[3], result.AccountMutations[0].Version);
        }

        [Fact]
        public void Parse_AssetDefinitions()
        {
            ParsedMutation result = Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.AssetDefinition, "/the/asset").BinaryData,
                new TextValue(BinaryValueUsage.Text, "Definition").BinaryData,
                binaryData[3]));

            Assert.Equal(0, result.AccountMutations.Count);
            Assert.Equal(1, result.AssetDefinitions.Count);
            Assert.Equal(0, result.Aliases.Count);
            Assert.Equal("/the/asset", result.AssetDefinitions[0].Key.FullPath);
            Assert.Equal("Definition", result.AssetDefinitions[0].Value);
        }

        [Fact]
        public void Parse_Alias()
        {
            ParsedMutation result = Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.Alias, "alias").BinaryData,
                new TextValue(BinaryValueUsage.Text, "/the/path").BinaryData,
                binaryData[3]));

            Assert.Equal(0, result.AccountMutations.Count);
            Assert.Equal(0, result.AssetDefinitions.Count);
            Assert.Equal(1, result.Aliases.Count);
            Assert.Equal("alias", result.Aliases[0].Key);
            Assert.Equal("/the/path", result.Aliases[0].Value.FullPath);
        }

        [Fact]
        public void Parse_InvalidKeyPair()
        {
            // Invalid usages
            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new AccountKey("/the/account", "/the/asset").BinaryData,
                new TextValue(BinaryValueUsage.Text, "Definition").BinaryData,
                binaryData[3])));

            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.AssetDefinition, "/the/asset").BinaryData,
                new Int64Value(100).BinaryData,
                binaryData[3])));

            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.Text, "Text").BinaryData,
                new Int64Value(100).BinaryData,
                binaryData[3])));

            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.Alias, "alias").BinaryData,
                new Int64Value(100).BinaryData,
                binaryData[3])));

            // Empty value
            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new AccountKey("/the/account", "/the/asset").BinaryData,
                BinaryData.Empty,
                binaryData[3])));

            // Invalid path
            byte[] key = new AccountKey("/account", "/the/asset").BinaryData.ToByteArray();
            key[8] = (byte)'a';
            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new BinaryData(key),
                new Int64Value(100).BinaryData,
                binaryData[3])));

            // Invalid alias
            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.Alias, "alias").BinaryData,
                new TextValue(BinaryValueUsage.Text, "the/path").BinaryData,
                binaryData[3])));

            // Invalid Binary Value
            Assert.Throws<TransactionInvalidException>(() => Parse(new KeyValuePair(
                new TextValue(BinaryValueUsage.AccountKey, "Text").BinaryData,
                new Int64Value(100).BinaryData,
                binaryData[3])));
        }

        private ParsedMutation Parse(params KeyValuePair[] pairs)
        {
            Mutation mutation = new Mutation(
                binaryData[1],
                pairs,
                binaryData[2]);

            return ParsedMutation.Parse(mutation);
        }
    }
}
