using Google.ProtocolBuffers;
using System;
using System.Linq;

namespace OpenChain.Core
{
    public static class MessageSerializer
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] SerializeTransaction(Transaction transaction)
        {
            Messages.Transaction.Builder transactionBuilder = new Messages.Transaction.Builder()
            {
                Metadata = Google.ProtocolBuffers.ByteString.CopyFrom(transaction.Metadata.ToArray())
            };

            transactionBuilder.AddRangeAccountEntries(
                transaction.AccountEntries.Select(
                    operation => new Messages.Transaction.Types.AccountEntry.Builder()
                    {
                        Account = operation.AccountKey.Account,
                        Asset = operation.AccountKey.Asset,
                        Amount = operation.Amount,
                        Version = ByteString.CopyFrom(operation.Version.ToArray())
                    }.Build()));

            return transactionBuilder.Build().ToByteArray();
        }

        public static Transaction DeserializeTransaction(byte[] data)
        {
            Messages.Transaction transaction = new Messages.Transaction.Builder().MergeFrom(data).BuildParsed();

            return new Transaction(
                transaction.AccountEntriesList.Select(
                    entry => new AccountEntry(
                        new AccountKey(entry.Account, entry.Asset),
                        entry.Amount,
                        new BinaryData(entry.Version))),
                new BinaryData(transaction.Metadata));
        }

        public static byte[] SerializeLedgerRecord(LedgerRecord record)
        {
            Messages.LedgerRecord.Builder recordBuilder = new Messages.LedgerRecord.Builder()
            {
                Transaction = ByteString.CopyFrom(record.Transaction.ToArray()),
                Timestamp = (long)(record.Timestamp - epoch).TotalSeconds,
                ExternalMetadata = ByteString.CopyFrom(record.ExternalMetadata.Value.ToArray()),
                PreviousRecord = ByteString.CopyFrom(record.PreviousRecordHash.Value.ToArray())
            };

            return recordBuilder.Build().ToByteArray();
        }

        public static LedgerRecord DeserializeLedgerRecord(byte[] data)
        {
            Messages.LedgerRecord record = new Messages.LedgerRecord.Builder().MergeFrom(data).BuildParsed();

            return new LedgerRecord(
                new BinaryData(record.Transaction.ToByteArray()),
                epoch + TimeSpan.FromSeconds(record.Timestamp),
                new BinaryData(record.ExternalMetadata.ToByteArray()),
                new BinaryData(record.PreviousRecord.ToByteArray()));
        }
    }
}
