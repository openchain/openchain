using System;
using System.Linq;
using Xunit;

namespace OpenChain.Core.Tests
{
    public class MutationTests
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
                    new KeyValuePair(binaryData[4], null, binaryData[5]),
                },
                binaryData[6]);

            Assert.Equal(2, mutation.KeyValuePairs.Count);
            Assert.Equal(binaryData[1], mutation.KeyValuePairs[0].Key);
            Assert.Equal(binaryData[2], mutation.KeyValuePairs[0].Value);
            Assert.Equal(binaryData[3], mutation.KeyValuePairs[0].Version);
            Assert.Equal(binaryData[4], mutation.KeyValuePairs[1].Key);
            Assert.Equal(null, mutation.KeyValuePairs[1].Value);
            Assert.Equal(binaryData[5], mutation.KeyValuePairs[1].Version);
            Assert.Equal(binaryData[0], mutation.Namespace);
            Assert.Equal(binaryData[6], mutation.Metadata);
        }

        [Fact]
        public void Mutation_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Mutation(
                null,
                new[] { new KeyValuePair(binaryData[1], binaryData[2], binaryData[3]) },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                null,
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[] { new KeyValuePair(binaryData[1], binaryData[2], binaryData[3]) },
                null));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[] { new KeyValuePair(binaryData[1], binaryData[2], binaryData[3]), null },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[] { new KeyValuePair(binaryData[1], null, binaryData[3]) },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() =>
                new KeyValuePair(null, binaryData[2], binaryData[3]));

            Assert.Throws<ArgumentNullException>(() =>
                new KeyValuePair(binaryData[1], binaryData[2], null));
        }
    }
}
