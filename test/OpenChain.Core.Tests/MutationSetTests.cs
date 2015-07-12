using System;
using System.Linq;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class MutationSetTests
    {
        private readonly BinaryData[] binaryData =
            Enumerable.Range(0, 10).Select(index => new BinaryData(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void MutationSet_Success()
        {
            MutationSet mutationSet = new MutationSet(
                binaryData[0],
                new[]
                {
                    new Mutation(binaryData[1], binaryData[2], binaryData[3]),
                    new Mutation(binaryData[4], binaryData[5], binaryData[6]),
                },
                binaryData[7]);

            Assert.Equal(2, mutationSet.Mutations.Count);
            Assert.Equal(binaryData[1], mutationSet.Mutations[0].Key);
            Assert.Equal(binaryData[2], mutationSet.Mutations[0].Value);
            Assert.Equal(binaryData[3], mutationSet.Mutations[0].Version);
            Assert.Equal(binaryData[0], mutationSet.Namespace);
            Assert.Equal(binaryData[7], mutationSet.Metadata);
        }

        [Fact]
        public void MutationSet_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MutationSet(
                null,
                new[] { new Mutation(binaryData[1], binaryData[2], binaryData[3]) },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new MutationSet(
                binaryData[0],
                null,
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new MutationSet(
                binaryData[0],
                new[] { new Mutation(binaryData[1], binaryData[2], binaryData[3]) },
                null));

            Assert.Throws<ArgumentNullException>(() => new MutationSet(
                binaryData[0],
                new[] { new Mutation(binaryData[1], binaryData[2], binaryData[3]), null },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() =>
                new Mutation(null, binaryData[2], binaryData[3]));

            Assert.Throws<ArgumentNullException>(() =>
                new Mutation(binaryData[1], null, binaryData[3]));

            Assert.Throws<ArgumentNullException>(() =>
                new Mutation(binaryData[1], binaryData[2], null));
        }
    }
}
