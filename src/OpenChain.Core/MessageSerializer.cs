using System;
using System.Linq;
using System.Security.Cryptography;
using Google.ProtocolBuffers;

namespace OpenChain.Core
{
    public static class MessageSerializer
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] SerializeMutation(Mutation mutation)
        {
            Messages.Mutation.Builder mutationBuilder = new Messages.Mutation.Builder()
            {
                Namespace = mutation.Namespace.ToByteString(),
                Metadata = mutation.Metadata.ToByteString()
            };

            mutationBuilder.AddRangeRecords(
                mutation.Records.Select(
                    record =>
                    {
                        var builder = new Messages.Record.Builder()
                        {
                            Key = record.Key.ToByteString(),
                            Version = record.Version.ToByteString()
                        };

                        if (record.Value != null)
                            builder.Value = record.Value.ToByteString();

                        return builder.Build();
                    }));

            return mutationBuilder.BuildParsed().ToByteArray();
        }

        public static Mutation DeserializeMutation(BinaryData data)
        {
            Messages.Mutation mutation = new Messages.Mutation.Builder().MergeFrom(data.ToByteString()).BuildParsed();

            return new Mutation(
                new BinaryData(ByteString.Unsafe.GetBuffer(mutation.Namespace)),
                mutation.RecordsList.Select(
                    record => new Record(
                        new BinaryData(ByteString.Unsafe.GetBuffer(record.Key)),
                        record.HasValue ? new BinaryData(ByteString.Unsafe.GetBuffer(record.Value)) : null,
                        new BinaryData(ByteString.Unsafe.GetBuffer(record.Version)))),
                new BinaryData(ByteString.Unsafe.GetBuffer(mutation.Metadata)));
        }

        public static byte[] SerializeTransaction(Transaction transaction)
        {
            Messages.Transaction.Builder transactionBuilder = new Messages.Transaction.Builder()
            {
                Mutation = transaction.Mutation.ToByteString(),
                Timestamp = (long)(transaction.Timestamp - epoch).TotalSeconds,
                TransactionMetadata = transaction.TransactionMetadata.ToByteString()
            };

            return transactionBuilder.BuildParsed().ToByteArray();
        }

        public static Transaction DeserializeTransaction(BinaryData data)
        {
            Messages.Transaction record = new Messages.Transaction.Builder().MergeFrom(data.ToByteString()).BuildParsed();

            return new Transaction(
                new BinaryData(ByteString.Unsafe.GetBuffer(record.Mutation)),
                epoch + TimeSpan.FromSeconds(record.Timestamp),
                new BinaryData(ByteString.Unsafe.GetBuffer(record.TransactionMetadata)));
        }
        
        public static byte[] ComputeHash(byte[] data)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(hash.ComputeHash(data));
            }
        }
    }
}
