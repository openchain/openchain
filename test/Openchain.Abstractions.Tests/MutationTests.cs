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
using Xunit;

namespace Openchain.Tests
{
    public class MutationTests
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

            Assert.Equal(2, mutation.Records.Count);
            Assert.Equal(binaryData[1], mutation.Records[0].Key);
            Assert.Equal(binaryData[2], mutation.Records[0].Value);
            Assert.Equal(binaryData[3], mutation.Records[0].Version);
            Assert.Equal(binaryData[4], mutation.Records[1].Key);
            Assert.Equal(null, mutation.Records[1].Value);
            Assert.Equal(binaryData[5], mutation.Records[1].Version);
            Assert.Equal(binaryData[0], mutation.Namespace);
            Assert.Equal(binaryData[6], mutation.Metadata);
        }

        [Fact]
        public void Mutation_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Mutation(
                null,
                new[] { new Record(binaryData[1], binaryData[2], binaryData[3]) },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                null,
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[] { new Record(binaryData[1], binaryData[2], binaryData[3]) },
                null));

            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[] { new Record(binaryData[1], binaryData[2], binaryData[3]), null },
                binaryData[4]));

            Assert.Throws<ArgumentNullException>(() =>
                new Record(null, binaryData[2], binaryData[3]));

            Assert.Throws<ArgumentNullException>(() =>
                new Record(binaryData[1], binaryData[2], null));
        }

        [Fact]
        public void Mutation_DuplicateKey()
        {
            Assert.Throws<ArgumentNullException>(() => new Mutation(
                binaryData[0],
                new[]
                {
                    new Record(binaryData[1], binaryData[2], binaryData[3]),
                    new Record(binaryData[1], binaryData[4], binaryData[5]),
                },
                binaryData[6]));
        }
    }
}
