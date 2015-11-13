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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Openchain.Ledger.Tests
{
    public class LedgerQueriesExtensionsTests
    {
        private ILedgerQueries store;

        [Fact]
        public async Task GetRecordVersion_Success()
        {
            this.store = new TestLedgerQueries(CreateTransaction("a", "b"));

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Parse("1234"));

            Assert.Equal(new ByteString(Encoding.UTF8.GetBytes("b")), record.Key);
            Assert.Equal(ByteString.Parse("ab"), record.Value);
            Assert.Equal(ByteString.Parse("cd"), record.Version);
        }

        [Fact]
        public async Task GetRecordVersion_InitialVersion()
        {
            this.store = new TestLedgerQueries(CreateTransaction("a", "b"));

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Empty);

            Assert.Equal(new ByteString(Encoding.UTF8.GetBytes("b")), record.Key);
            Assert.Equal(ByteString.Empty, record.Value);
            Assert.Equal(ByteString.Empty, record.Version);
        }

        [Fact]
        public async Task GetRecordVersion_NonExistingTransaction()
        {
            this.store = new TestLedgerQueries(null);

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Parse("1234"));

            Assert.Null(record);
        }

        [Fact]
        public async Task GetRecordVersion_NonExistingMutation()
        {
            this.store = new TestLedgerQueries(CreateTransaction("a", "b"));

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("c")), ByteString.Parse("1234"));

            Assert.Null(record);
        }

        private ByteString CreateTransaction(params string[] keys)
        {
            Mutation mutation = new Mutation(
                ByteString.Empty,
                keys.Select(key => new Record(
                    new ByteString(Encoding.UTF8.GetBytes(key)),
                    ByteString.Parse("ab"),
                    ByteString.Parse("cd"))),
                ByteString.Empty);

            byte[] serializedMutation = MessageSerializer.SerializeMutation(mutation);

            Transaction transaction = new Transaction(
                new ByteString(MessageSerializer.SerializeMutation(mutation)),
                new DateTime(),
                ByteString.Empty);

            return new ByteString(MessageSerializer.SerializeTransaction(transaction));
        }

        private class TestLedgerQueries : ILedgerQueries
        {
            private readonly ByteString transaction;

            public TestLedgerQueries(ByteString transaction)
            {
                this.transaction = transaction;
            }

            public Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
            {
                throw new NotImplementedException();
            }

            public Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey)
            {
                throw new NotImplementedException();
            }

            public Task<ByteString> GetTransaction(ByteString mutationHash)
            {
                return Task.FromResult(this.transaction);
            }
        }
    }
}
