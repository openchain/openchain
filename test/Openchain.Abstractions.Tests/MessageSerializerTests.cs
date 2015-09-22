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
using Google.Protobuf;
using Xunit;

namespace Openchain.Tests
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

            Assert.Equal(244, result.Length);
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
            Assert.Throws<InvalidProtocolBufferException>(() => MessageSerializer.DeserializeMutation(ByteString.Parse("01")));
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
            Assert.Throws<InvalidProtocolBufferException>(() => MessageSerializer.DeserializeTransaction(ByteString.Parse("01")));
        }
    }
}