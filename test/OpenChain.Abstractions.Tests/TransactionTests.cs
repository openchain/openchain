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

namespace OpenChain.Tests
{
    public class TransactionTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Transaction_Success()
        {
            Transaction record = new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]);

            Assert.Equal(binaryData[0], record.Mutation);
            Assert.Equal(new DateTime(1, 2, 3, 4, 5, 6), record.Timestamp);
            Assert.Equal(binaryData[1], record.TransactionMetadata);
        }

        [Fact]
        public void Transaction_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Transaction(
                null,
                new DateTime(1, 2, 3, 4, 5, 6),
                binaryData[1]));

            Assert.Throws<ArgumentNullException>(() => new Transaction(
                binaryData[0],
                new DateTime(1, 2, 3, 4, 5, 6),
                null));
        }
    }
}
