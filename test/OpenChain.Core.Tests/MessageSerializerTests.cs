using System;
using System.Linq;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class MessageSerializerTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Mutation_Success()
        {
            Mutation mutation = new Mutation(
                binaryData[0],
                new[]
                {
                    new KeyValuePair(binaryData[1], binaryData[2], binaryData[3]),
                    new KeyValuePair(binaryData[4], binaryData[5], binaryData[6]),
                },
                binaryData[7]);

            byte[] result = MessageSerializer.SerializeMutation(mutation);

            Mutation finalMutation = MessageSerializer.DeserializeMutation(new BinaryData(result));

            Assert.Equal(276, result.Length);
            Assert.Equal(mutation.KeyValuePairs.Count, finalMutation.KeyValuePairs.Count);
            Assert.Equal(mutation.KeyValuePairs[0].Key, finalMutation.KeyValuePairs[0].Key);
            Assert.Equal(mutation.KeyValuePairs[0].Value, finalMutation.KeyValuePairs[0].Value);
            Assert.Equal(mutation.KeyValuePairs[0].Version, finalMutation.KeyValuePairs[0].Version);
            Assert.Equal(mutation.KeyValuePairs[1].Key, finalMutation.KeyValuePairs[1].Key);
            Assert.Equal(mutation.KeyValuePairs[1].Value, finalMutation.KeyValuePairs[1].Value);
            Assert.Equal(mutation.KeyValuePairs[1].Version, finalMutation.KeyValuePairs[1].Version);
            Assert.Equal(mutation.Namespace, finalMutation.Namespace);
            Assert.Equal(mutation.Metadata, finalMutation.Metadata);
        }

        [Fact]
        public void Transaction_Success()
        {
            Transaction transaction = new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]);

            byte[] result = MessageSerializer.SerializeTransaction(transaction);

            Transaction finalTransaction = MessageSerializer.DeserializeTransaction(new BinaryData(result));

            Assert.Equal(79, result.Length);
            Assert.Equal(transaction.Mutation, finalTransaction.Mutation);
            Assert.Equal(transaction.Timestamp, finalTransaction.Timestamp);
            Assert.Equal(transaction.TransactionMetadata, finalTransaction.TransactionMetadata);
        }
    }
}