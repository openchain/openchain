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
        public void MutationSet()
        {
            MutationSet mutationSet = new MutationSet(
                binaryData[0],
                new[]
                {
                    new Mutation(binaryData[1], binaryData[2], binaryData[3]),
                    new Mutation(binaryData[4], binaryData[5], binaryData[6]),
                },
                binaryData[7]);

            byte[] result = MessageSerializer.SerializeMutationSet(mutationSet);

            MutationSet finalMutationSet = MessageSerializer.DeserializeMutationSet(new BinaryData(result));

            Assert.Equal(276, result.Length);
            Assert.Equal(mutationSet.Mutations.Count, finalMutationSet.Mutations.Count);
            Assert.Equal(mutationSet.Mutations[0].Key, finalMutationSet.Mutations[0].Key);
            Assert.Equal(mutationSet.Mutations[0].Value, finalMutationSet.Mutations[0].Value);
            Assert.Equal(mutationSet.Mutations[0].Version, finalMutationSet.Mutations[0].Version);
            Assert.Equal(mutationSet.Mutations[1].Key, finalMutationSet.Mutations[1].Key);
            Assert.Equal(mutationSet.Mutations[1].Value, finalMutationSet.Mutations[1].Value);
            Assert.Equal(mutationSet.Mutations[1].Version, finalMutationSet.Mutations[1].Version);
            Assert.Equal(mutationSet.Namespace, finalMutationSet.Namespace);
            Assert.Equal(mutationSet.Metadata, finalMutationSet.Metadata);
        }

        [Fact]
        public void Transaction()
        {
            Transaction transaction = new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]);

            byte[] result = MessageSerializer.SerializeTransaction(transaction);

            Transaction finalTransaction = MessageSerializer.DeserializeTransaction(new BinaryData(result));

            Assert.Equal(79, result.Length);
            Assert.Equal(transaction.MutationSet, finalTransaction.MutationSet);
            Assert.Equal(transaction.Timestamp, finalTransaction.Timestamp);
            Assert.Equal(transaction.TransactionMetadata, finalTransaction.TransactionMetadata);
        }
    }
}