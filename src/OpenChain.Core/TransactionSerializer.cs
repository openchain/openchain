using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OpenChain.Core
{
    public static class TransactionSerializer
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] SerializeTransaction(Transaction transaction)
        {
            SerializableTransaction serializableTransaction = new SerializableTransaction()
            {
                AccountEntries = transaction.AccountEntries.Select(
                    operation =>
                        new SerializableAccountEntry()
                        {
                            Account = operation.AccountKey.Account,
                            Asset = operation.AccountKey.Asset,
                            Amount = operation.Amount,
                            Version = operation.Version.Value.ToArray()
                        })
                        .ToArray(),
                Metadata = transaction.Metadata.Value.ToArray()
            };

            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize<SerializableTransaction>(stream, serializableTransaction);
                return stream.ToArray();
            }
        }

        public static Transaction DeserializeTransaction(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                SerializableTransaction result = Serializer.Deserialize<SerializableTransaction>(stream);

                return new Transaction(
                    result.AccountEntries.Select(
                        entry => new AccountEntry(
                            new AccountKey(entry.Account, entry.Asset),
                            entry.Amount,
                            new BinaryData(entry.Version))),
                    new BinaryData(result.Metadata));
            }
        }

        public static byte[] SerializeLedgerRecord(this LedgerRecord record)
        {
            SerializableLedgerRecord serializableRecord = new SerializableLedgerRecord()
            {
                Transaction = record.Payload.Value.ToArray(),
                Timestamp = (long)(record.Timestamp - epoch).TotalSeconds,
                ExternalMetadata = record.ExternalMetadata.Value.ToArray(),
                PreviousRecord = record.PreviousRecordHash.Value.ToArray()
            };

            using (MemoryStream stream = new MemoryStream())
            {
                Serializer.Serialize<SerializableLedgerRecord>(stream, serializableRecord);
                return stream.ToArray();
            }
        }

        public static LedgerRecord DeserializeLedgerRecord(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                SerializableLedgerRecord result = Serializer.Deserialize<SerializableLedgerRecord>(stream);

                return new LedgerRecord(
                    new BinaryData(result.Transaction),
                    epoch + TimeSpan.FromSeconds(result.Timestamp),
                    new BinaryData(result.ExternalMetadata),
                    new BinaryData(result.PreviousRecord));
            }
        }

        #region Protocol Buffers Contracts

        [ProtoContract]
        private class SerializableTransaction
        {
            [ProtoMember(1, IsRequired = true)]
            public IList<SerializableAccountEntry> AccountEntries { get; set; }

            [ProtoMember(2, IsRequired = true)]
            public byte[] Metadata { get; set; }
        }

        [ProtoContract]
        private class SerializableAccountEntry
        {
            [ProtoMember(1, IsRequired = true)]
            public string Account { get; set; }

            [ProtoMember(2, IsRequired = true)]
            public string Asset { get; set; }

            [ProtoMember(3, IsRequired = true, DataFormat = DataFormat.ZigZag)]
            public long Amount { get; set; }

            [ProtoMember(4, IsRequired = true)]
            public byte[] Version { get; set; }
        }

        [ProtoContract]
        private class SerializableLedgerRecord
        {
            [ProtoMember(1, IsRequired = true)]
            public byte[] Transaction { get; set; }

            [ProtoMember(2, IsRequired = true)]
            public long Timestamp { get; set; }

            [ProtoMember(3, IsRequired = true)]
            public byte[] ExternalMetadata { get; set; }

            [ProtoMember(4, IsRequired = true)]
            public byte[] PreviousRecord { get; set; }
        }

        #endregion
    }
}
