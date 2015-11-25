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
        public async Task GetAccount_Success()
        {
            this.store = new GetKeyStartingFromLedgerQueries(
                CreateRecord("/path/to/account/:ACC:/asset/", 1),
                CreateRecord("/path/to/account/sub/:ACC:/asset/", 2),
                CreateRecord("/path/to/:ACC:/asset/", 3),
                new Record(new ByteString(Encoding.UTF8.GetBytes("/path/to/account/:ACC:/empty/")), ByteString.Empty, ByteString.Parse("1234")),
                CreateRecord("/path/to/account/:DATA:/asset/", 4),
                CreateRecord("/path/to/accounting/:ACC:/asset/", 5));

            IReadOnlyList<AccountStatus> result = await this.store.GetAccount("/path/to/account/");

            Assert.Equal(1, result.Count);
            Assert.Equal("/path/to/account/:ACC:/asset/", result[0].AccountKey.Key.ToString());
            Assert.Equal(1, result[0].Balance);
            Assert.Equal(ByteString.Parse("1234"), result[0].Version);
        }

        [Fact]
        public async Task GetSubaccounts_Success()
        {
            this.store = new GetKeyStartingFromLedgerQueries(
                CreateRecord("/path/to/account/:ACC:/asset/", 1),
                CreateRecord("/path/to/account/sub/:ACC:/asset/", 2),
                CreateRecord("/path/to/:ACC:/asset/", 3),
                CreateRecord("/path/to/account/:DATA:/asset/", 4),
                CreateRecord("/path/to/accounting/:ACC:/asset/", 5));

            IReadOnlyList<Record> result = await this.store.GetSubaccounts("/path/to/account/");

            Assert.Equal(3, result.Count);
            Assert.Equal("/path/to/account/:ACC:/asset/", Encoding.UTF8.GetString(result[0].Key.Value.ToArray()));
            Assert.Equal(1, BitConverter.ToInt64(result[0].Value.Value.Reverse().ToArray(), 0));
            Assert.Equal(ByteString.Parse("1234"), result[0].Version);
            Assert.Equal("/path/to/account/sub/:ACC:/asset/", Encoding.UTF8.GetString(result[1].Key.Value.ToArray()));
            Assert.Equal(2, BitConverter.ToInt64(result[1].Value.Value.Reverse().ToArray(), 0));
            Assert.Equal(ByteString.Parse("1234"), result[1].Version);
            Assert.Equal("/path/to/account/:DATA:/asset/", Encoding.UTF8.GetString(result[2].Key.Value.ToArray()));
            Assert.Equal(4, BitConverter.ToInt64(result[2].Value.Value.Reverse().ToArray(), 0));
            Assert.Equal(ByteString.Parse("1234"), result[2].Version);
        }

        [Fact]
        public async Task GetRecordVersion_Success()
        {
            this.store = new GetTransactionLedgerQueries(CreateTransaction("a", "b"));

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Parse("1234"));

            Assert.Equal(new ByteString(Encoding.UTF8.GetBytes("b")), record.Key);
            Assert.Equal(ByteString.Parse("ab"), record.Value);
            Assert.Equal(ByteString.Parse("cd"), record.Version);
        }

        [Fact]
        public async Task GetRecordVersion_InitialVersion()
        {
            this.store = new GetTransactionLedgerQueries(CreateTransaction("a", "b"));

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Empty);

            Assert.Equal(new ByteString(Encoding.UTF8.GetBytes("b")), record.Key);
            Assert.Equal(ByteString.Empty, record.Value);
            Assert.Equal(ByteString.Empty, record.Version);
        }

        [Fact]
        public async Task GetRecordVersion_NonExistingTransaction()
        {
            this.store = new GetTransactionLedgerQueries(null);

            Record record = await this.store.GetRecordVersion(new ByteString(Encoding.UTF8.GetBytes("b")), ByteString.Parse("1234"));

            Assert.Null(record);
        }

        [Fact]
        public async Task GetRecordVersion_NonExistingMutation()
        {
            this.store = new GetTransactionLedgerQueries(CreateTransaction("a", "b"));

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

        private static Record CreateRecord(string key, long value)
        {
            return new Record(
                new ByteString(Encoding.UTF8.GetBytes(key)),
                new ByteString(BitConverter.GetBytes(value).Reverse()),
                ByteString.Parse("1234"));
        }

        private class GetKeyStartingFromLedgerQueries : ILedgerQueries
        {
            private readonly IList<Record> records;

            public GetKeyStartingFromLedgerQueries(params Record[] records)
            {
                this.records = records;
            }

            public Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
            {
                return Task.FromResult((IReadOnlyList<Record>)records.Where(record => record.Key.ToString().StartsWith(prefix.ToString())).ToList());
            }

            public Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey)
            {
                throw new NotImplementedException();
            }

            public Task<ByteString> GetTransaction(ByteString mutationHash)
            {
                throw new NotImplementedException();
            }
        }

        private class GetTransactionLedgerQueries : ILedgerQueries
        {
            private readonly ByteString transaction;

            public GetTransactionLedgerQueries(ByteString transaction)
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
