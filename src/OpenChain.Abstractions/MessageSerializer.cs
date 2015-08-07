using System;
using System.Linq;
using System.Security.Cryptography;
using Google.ProtocolBuffers;

namespace OpenChain
{
    public static class MessageSerializer
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Serializes a <see cref="Mutation"/> into a byte array.
        /// </summary>
        /// <param name="mutation">The mutation to serialize.</param>
        /// <returns>The serialized mutation.</returns>
        public static byte[] SerializeMutation(Mutation mutation)
        {
            Messages.Mutation.Builder mutationBuilder = new Messages.Mutation.Builder()
            {
                Namespace = mutation.Namespace.ToProtocolBuffers(),
                Metadata = mutation.Metadata.ToProtocolBuffers()
            };

            mutationBuilder.AddRangeRecords(
                mutation.Records.Select(
                    record =>
                    {
                        var builder = new Messages.Record.Builder()
                        {
                            Key = record.Key.ToProtocolBuffers(),
                            Version = record.Version.ToProtocolBuffers()
                        };

                        if (record.Value != null)
                            builder.Value = record.Value.ToProtocolBuffers();

                        return builder.Build();
                    }));

            return mutationBuilder.BuildParsed().ToByteArray();
        }

        /// <summary>
        /// Deserialize a <see cref="Mutation"/> from binary data.
        /// </summary>
        /// <param name="data">The binary data to deserialize.</param>
        /// <returns>The deserialized <see cref="Mutation"/>.</returns>
        public static Mutation DeserializeMutation(ByteString data)
        {
            Messages.Mutation mutation = new Messages.Mutation.Builder().MergeFrom(data.ToProtocolBuffers()).BuildParsed();

            return new Mutation(
                new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(mutation.Namespace)),
                mutation.RecordsList.Select(
                    record => new Record(
                        new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(record.Key)),
                        record.HasValue ? new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(record.Value)) : null,
                        new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(record.Version)))),
                new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(mutation.Metadata)));
        }

        /// <summary>
        /// Serializes a <see cref="Transaction"/> into a byte array.
        /// </summary>
        /// <param name="transaction">The transaction to serialize.</param>
        /// <returns>The serialized transaction.</returns>
        public static byte[] SerializeTransaction(Transaction transaction)
        {
            Messages.Transaction.Builder transactionBuilder = new Messages.Transaction.Builder()
            {
                Mutation = transaction.Mutation.ToProtocolBuffers(),
                Timestamp = (long)(transaction.Timestamp - epoch).TotalSeconds,
                TransactionMetadata = transaction.TransactionMetadata.ToProtocolBuffers()
            };

            return transactionBuilder.BuildParsed().ToByteArray();
        }

        /// <summary>
        /// Deserialize a <see cref="Transaction"/> from binary data.
        /// </summary>
        /// <param name="data">The binary data to deserialize.</param>
        /// <returns>The deserialized <see cref="Transaction"/>.</returns>
        public static Transaction DeserializeTransaction(ByteString data)
        {
            Messages.Transaction record = new Messages.Transaction.Builder().MergeFrom(data.ToProtocolBuffers()).BuildParsed();

            return new Transaction(
                new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(record.Mutation)),
                epoch + TimeSpan.FromSeconds(record.Timestamp),
                new ByteString(Google.ProtocolBuffers.ByteString.Unsafe.GetBuffer(record.TransactionMetadata)));
        }
        
        /// <summary>
        /// Calculates the hash of an array of bytes.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>The result of the hash.</returns>
        public static byte[] ComputeHash(byte[] data)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return hash.ComputeHash(hash.ComputeHash(data));
            }
        }
    }
}
