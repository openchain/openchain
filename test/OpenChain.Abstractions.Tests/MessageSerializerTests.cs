using System;
using System.Linq;
using Google.ProtocolBuffers;
using Xunit;

namespace OpenChain.Tests
{
    public class MessageSerializerTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Mutation_Success()
        {
            Mutation mutation = new Mutation(
                binaryData[0],
                new[]
                {
                    new Record(binaryData[1], binaryData[2], binaryData[3]),
                    new Record(binaryData[4], null, binaryData[5]),
                },
                binaryData[6]);

            byte[] result = MessageSerializer.SerializeMutation(mutation);

            Mutation finalMutation = MessageSerializer.DeserializeMutation(new ByteString(result));

            Assert.Equal(242, result.Length);
            Assert.Equal(mutation.Records.Count, finalMutation.Records.Count);
            Assert.Equal(mutation.Records[0].Key, finalMutation.Records[0].Key);
            Assert.Equal(mutation.Records[0].Value, finalMutation.Records[0].Value);
            Assert.Equal(mutation.Records[0].Version, finalMutation.Records[0].Version);
            Assert.Equal(mutation.Records[1].Key, finalMutation.Records[1].Key);
            Assert.Equal(mutation.Records[1].Value, finalMutation.Records[1].Value);
            Assert.Equal(mutation.Records[1].Version, finalMutation.Records[1].Version);
            Assert.Equal(mutation.Namespace, finalMutation.Namespace);
            Assert.Equal(mutation.Metadata, finalMutation.Metadata);
        }

        [Fact]
        public void Mutation_Invalid()
        {
            Assert.Throws<InvalidProtocolBufferException>(() => MessageSerializer.DeserializeMutation(ByteString.Empty));
        }

        [Fact]
        public void Transaction_Success()
        {
            Transaction transaction = new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]);

            byte[] result = MessageSerializer.SerializeTransaction(transaction);

            Transaction finalTransaction = MessageSerializer.DeserializeTransaction(new ByteString(result));

            Assert.Equal(79, result.Length);
            Assert.Equal(transaction.Mutation, finalTransaction.Mutation);
            Assert.Equal(transaction.Timestamp, finalTransaction.Timestamp);
            Assert.Equal(transaction.TransactionMetadata, finalTransaction.TransactionMetadata);
        }

        [Fact]
        public void Transaction_Invalid()
        {
            Assert.Throws<InvalidProtocolBufferException>(() => MessageSerializer.DeserializeTransaction(ByteString.Empty));
        }
    }
}