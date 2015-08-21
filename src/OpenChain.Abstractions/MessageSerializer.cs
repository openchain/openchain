// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Security.Cryptography;
using Google.Protobuf;

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
            Messages.Mutation mutationBuilder = new Messages.Mutation()
            {
                Namespace = mutation.Namespace.ToProtocolBuffers(),
                Metadata = mutation.Metadata.ToProtocolBuffers()
            };

            mutationBuilder.Records.Add(
                mutation.Records.Select(
                    record =>
                    {
                        var builder = new Messages.Record()
                        {
                            Key = record.Key.ToProtocolBuffers(),
                            Version = record.Version.ToProtocolBuffers()
                        };

                        if (record.Value != null)
                            builder.Value = new Messages.RecordValue() { Data = record.Value.ToProtocolBuffers() };

                        return builder;
                    }));

            return mutationBuilder.ToByteArray();
        }

        /// <summary>
        /// Deserialize a <see cref="Mutation"/> from binary data.
        /// </summary>
        /// <param name="data">The binary data to deserialize.</param>
        /// <returns>The deserialized <see cref="Mutation"/>.</returns>
        public static Mutation DeserializeMutation(ByteString data)
        {
            Messages.Mutation mutation = new Messages.Mutation();
            mutation.MergeFrom(data.ToProtocolBuffers());
            
            return new Mutation(
                new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(mutation.Namespace)),
                mutation.Records.Select(
                    record => new Record(
                        new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(record.Key)),
                        record.Value != null ? new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(record.Value.Data)) : null,
                        new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(record.Version)))),
                new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(mutation.Metadata)));
        }

        /// <summary>
        /// Serializes a <see cref="Transaction"/> into a byte array.
        /// </summary>
        /// <param name="transaction">The transaction to serialize.</param>
        /// <returns>The serialized transaction.</returns>
        public static byte[] SerializeTransaction(Transaction transaction)
        {
            Messages.Transaction transactionBuilder = new Messages.Transaction()
            {
                Mutation = transaction.Mutation.ToProtocolBuffers(),
                Timestamp = (long)(transaction.Timestamp - epoch).TotalSeconds,
                TransactionMetadata = transaction.TransactionMetadata.ToProtocolBuffers()
            };

            return transactionBuilder.ToByteArray();
        }

        /// <summary>
        /// Deserialize a <see cref="Transaction"/> from binary data.
        /// </summary>
        /// <param name="data">The binary data to deserialize.</param>
        /// <returns>The deserialized <see cref="Transaction"/>.</returns>
        public static Transaction DeserializeTransaction(ByteString data)
        {
            Messages.Transaction transaction = new Messages.Transaction();
            transaction.MergeFrom(data.ToProtocolBuffers());

            return new Transaction(
                new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(transaction.Mutation)),
                epoch + TimeSpan.FromSeconds(transaction.Timestamp),
                new ByteString(Google.Protobuf.ByteString.Unsafe.GetBuffer(transaction.TransactionMetadata)));
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
