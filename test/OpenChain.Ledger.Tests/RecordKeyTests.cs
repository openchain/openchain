using System;
using System.Linq;
using System.Text;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class RecordKeyTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Parse_Account()
        {
            BinaryData data = new BinaryData(Encoding.UTF8.GetBytes("/account/name:ACC:/asset/name"));
            RecordKey key = RecordKey.Parse(data);

            Assert.Equal(RecordType.Account, key.RecordType);
            Assert.Equal("/account/name", key.Path.FullPath);
            Assert.Equal(1, key.AdditionalKeyComponents.Count);
            Assert.Equal("/asset/name", key.AdditionalKeyComponents[0].FullPath);
        }

        [Fact]
        public void Parse_AssetDefinition()
        {
            BinaryData data = new BinaryData(Encoding.UTF8.GetBytes("/asset/name:ASDEF"));
            RecordKey key = RecordKey.Parse(data);

            Assert.Equal(RecordType.AssetDefinition, key.RecordType);
            Assert.Equal("/asset/name", key.Path.FullPath);
            Assert.Equal(0, key.AdditionalKeyComponents.Count);
        }

        [Fact]
        public void Parse_Data()
        {
            BinaryData data = new BinaryData(Encoding.UTF8.GetBytes("/aka/name:DATA"));
            RecordKey key = RecordKey.Parse(data);

            Assert.Equal(RecordType.Data, key.RecordType);
            Assert.Equal("/aka/name", key.Path.FullPath);
            Assert.Equal(0, key.AdditionalKeyComponents.Count);
        }

        [Fact]
        public void Parse_Error()
        {
            // Invalid structure
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new BinaryData(Encoding.UTF8.GetBytes("/account/name"))));

            // Unknown record type
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new BinaryData(Encoding.UTF8.GetBytes("/account/name:DOESNOTEXIST"))));

            // Incorrect number of additional components
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new BinaryData(Encoding.UTF8.GetBytes("/asset/name:ASDEF:/other"))));

            // Incorrect number of additional components
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new BinaryData(Encoding.UTF8.GetBytes("/asset/name:ACC"))));

            // Invalid path
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new BinaryData(Encoding.UTF8.GetBytes("account/name:ACC"))));
        }

        [Fact]
        public void GetRecordTypeName_Success()
        {
            Assert.Equal("ACC", RecordKey.GetRecordTypeName(RecordType.Account));
            Assert.Equal("ASDEF", RecordKey.GetRecordTypeName(RecordType.AssetDefinition));
            Assert.Equal("DATA", RecordKey.GetRecordTypeName(RecordType.Data));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecordKey.GetRecordTypeName((RecordType)100000));
        }

        private static BinaryData SerializeInt(long value)
        {
            return new BinaryData(BitConverter.GetBytes(value).Reverse());
        }
    }
}
