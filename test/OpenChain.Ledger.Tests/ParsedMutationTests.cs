using System;
using System.Linq;
using System.Text;
using Assert = Xunit.Assert;
using Fact = Xunit.FactAttribute;

namespace OpenChain.Ledger.Tests
{
    public class ParsedMutationTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Parse_AccountMutations()
        {
            ParsedMutation result = Parse(new Record(
                SerializeString("/the/account:ACC:/the/asset"),
                SerializeInt(100),
                binaryData[3]));

            Assert.Equal(1, result.AccountMutations.Count);
            Assert.Equal(0, result.DataRecords.Count);
            Assert.Equal("/the/account", result.AccountMutations[0].AccountKey.Account.FullPath);
            Assert.Equal("/the/asset", result.AccountMutations[0].AccountKey.Asset.FullPath);
            Assert.Equal(100, result.AccountMutations[0].Balance);
            Assert.Equal(binaryData[3], result.AccountMutations[0].Version);
        }

        [Fact]
        public void Parse_Data()
        {
            ParsedMutation result = Parse(new Record(
                SerializeString("/aka/alias:DATA"),
                BinaryData.Parse("aabbccdd"),
                binaryData[3]));

            Assert.Equal(0, result.AccountMutations.Count);
            Assert.Equal(1, result.DataRecords.Count);
            Assert.Equal("/aka/alias", result.DataRecords[0].Key.FullPath);
            Assert.Equal(BinaryData.Parse("aabbccdd"), result.DataRecords[0].Value);
        }

        [Fact]
        public void Parse_OptimisticConcurrency()
        {
            ParsedMutation result = Parse(new Record(
                SerializeString("/the/account:ACC:/the/asset"),
                null,
                binaryData[3]));

            Assert.Equal(0, result.AccountMutations.Count);
            Assert.Equal(0, result.DataRecords.Count);
        }

        [Fact]
        public void Parse_InvalidRecord()
        {
            // Invalid number of components
            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("/the/account:ACC"),
                SerializeInt(100),
                binaryData[3])));

            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("/the/asset:ASDEF:/other/path"),
                SerializeString("Definition"),
                binaryData[3])));

            // Invalid path
            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("the/account:ACC:/the/asset"),
                SerializeInt(100),
                binaryData[3])));

            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("/the/account:ACC:the/asset"),
                SerializeInt(100),
                binaryData[3])));

            // Invalid account balance
            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("/the/account:ACC:/the/asset"),
                SerializeString("01"),
                binaryData[3])));

            // Invalid alias
            Assert.Throws<TransactionInvalidException>(() => Parse(new Record(
                SerializeString("/aka/alias:ALIAS"),
                SerializeString("the/path"),
                binaryData[3])));
        }

        private ParsedMutation Parse(params Record[] records)
        {
            Mutation mutation = new Mutation(
                binaryData[1],
                records,
                binaryData[2]);

            return ParsedMutation.Parse(mutation);
        }

        private static BinaryData SerializeInt(long value)
        {
            return new BinaryData(BitConverter.GetBytes(value).Reverse());
        }

        private static BinaryData SerializeString(string value)
        {
            return new BinaryData(Encoding.UTF8.GetBytes(value));
        }
    }
}
