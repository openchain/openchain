using Google.ProtocolBuffers;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace OpenChain.Core
{
    public static class MessageSerializer
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static byte[] SerializeMutationSet(MutationSet mutationSet)
        {
            Messages.MutationSet.Builder mutationSetBuilder = new Messages.MutationSet.Builder()
            {
                Namespace = mutationSet.Namespace.ToByteString(),
                Metadata = mutationSet.Metadata.ToByteString()
            };

            mutationSetBuilder.AddRangeMutations(
                mutationSet.Mutations.Select(
                    mutation => new Messages.MutationSet.Types.Mutation.Builder()
                    {
                        Key = mutation.Key.ToByteString(),
                        Value = mutation.Key.ToByteString(),
                        Version = mutation.Version.ToByteString()
                    }.Build()));

            return mutationSetBuilder.Build().ToByteArray();
        }

        public static MutationSet DeserializeMutationSet(BinaryData data)
        {
            Messages.MutationSet mutationSet = new Messages.MutationSet.Builder().MergeFrom(data.ToByteString()).BuildParsed();

            return new MutationSet(
                new BinaryData(ByteString.Unsafe.GetBuffer(mutationSet.Namespace)),
                mutationSet.MutationsList.Select(
                    entry => new Mutation(
                        new BinaryData(ByteString.Unsafe.GetBuffer(entry.Key)),
                        new BinaryData(ByteString.Unsafe.GetBuffer(entry.Value)),
                        new BinaryData(ByteString.Unsafe.GetBuffer(entry.Version)))),
                new BinaryData(ByteString.Unsafe.GetBuffer(mutationSet.Metadata)));
        }

        public static byte[] SerializeTransaction(Transaction transaction)
        {
            Messages.Transaction.Builder transactionBuilder = new Messages.Transaction.Builder()
            {
                MutationSet = transaction.MutationSet.ToByteString(),
                Timestamp = (long)(transaction.Timestamp - epoch).TotalSeconds,
                RecordMetadata = transaction.ExternalMetadata.ToByteString()
            };

            return transactionBuilder.Build().ToByteArray();
        }

        public static Transaction DeserializeTransaction(BinaryData data)
        {
            Messages.Transaction record = new Messages.Transaction.Builder().MergeFrom(data.ToByteString()).BuildParsed();

            return new Transaction(
                new BinaryData(ByteString.Unsafe.GetBuffer(record.MutationSet)),
                epoch + TimeSpan.FromSeconds(record.Timestamp),
                new BinaryData(ByteString.Unsafe.GetBuffer(record.RecordMetadata)));
        }
        
        public static byte[] ComputeHash(BinaryData message)
        {
            using (SHA256 hash = SHA256.Create())
            {
                using (Stream stream = message.ToStream())
                {
                    return hash.ComputeHash(hash.ComputeHash(stream));
                }
            }
        }
    }
}
