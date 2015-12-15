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
using Openchain.Ledger.Validation;
using Xunit;

namespace Openchain.Ledger.Tests
{
    public class TransactionValidatorTests
    {
        private static readonly Dictionary<string, long> defaultAccounts = new Dictionary<string, long>()
        {
            ["/account/1/"] = 90,
            ["/account/2/"] = 110,
        };

        [Fact]
        public async Task PostTransaction_Success()
        {
            TransactionValidator validator = CreateValidator(defaultAccounts);
            ByteString mutation = CreateMutation("http://root/");

            ByteString result = await validator.PostTransaction(mutation, new SignatureEvidence[0]);

            Assert.Equal(32, result.Value.Count);
        }

        [Fact]
        public async Task PostTransaction_InvalidMutation()
        {
            TransactionValidator validator = CreateValidator(new Dictionary<string, long>());
            ByteString mutation = ByteString.Parse("aa");

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
            Assert.Equal("InvalidMutation", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_MaxKeySize()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>();

            TransactionValidator validator = CreateValidator(accounts);
            Mutation mutation = new Mutation(
                new ByteString(Encoding.UTF8.GetBytes("http://root/")),
                new Record[]
                {
                    new Record(
                        ByteString.Parse(new string('a', 513 * 2)),
                        new ByteString(BitConverter.GetBytes(100L).Reverse()),
                        ByteString.Empty)
                },
                ByteString.Empty);

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(new ByteString(MessageSerializer.SerializeMutation(mutation)), new SignatureEvidence[0]));
            Assert.Equal("InvalidMutation", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_EmptyMutation()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>();

            TransactionValidator validator = CreateValidator(accounts);
            Mutation mutation = new Mutation(
                new ByteString(Encoding.UTF8.GetBytes("http://root/")),
                new Record[0],
                ByteString.Empty);

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(new ByteString(MessageSerializer.SerializeMutation(mutation)), new SignatureEvidence[0]));
            Assert.Equal("InvalidMutation", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_UnbalancedTransaction()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>()
            {
                ["/account/1/"] = 100,
                ["/account/2/"] = 110,
            };

            TransactionValidator validator = CreateValidator(accounts);
            ByteString mutation = CreateMutation("http://root/");

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
            Assert.Equal("UnbalancedTransaction", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_InvalidNamespace()
        {
            TransactionValidator validator = CreateValidator(defaultAccounts);
            ByteString mutation = CreateMutation("http://wrong-root/");

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
            Assert.Equal("InvalidNamespace", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_ConcurrencyException()
        {
            TransactionValidator validator = new TransactionValidator(
                new TestStore(defaultAccounts, true),
                new TestValidator(false),
                "http://root/");

            ByteString mutation = CreateMutation("http://root/");

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
            Assert.Equal("OptimisticConcurrency", exception.Reason);
        }

        [Fact]
        public async Task PostTransaction_ValidationException()
        {
            TransactionValidator validator = new TransactionValidator(
                new TestStore(defaultAccounts, false),
                new TestValidator(true),
                "http://root/");

            ByteString mutation = CreateMutation("http://root/");

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
            Assert.Equal("Test", exception.Reason);
        }

        [Fact]
        public async Task Validate_ValidSignature()
        {
            TransactionValidator validator = CreateValidator(defaultAccounts);
            ByteString mutation = CreateMutation("http://root/");

            SignatureEvidence signature = new SignatureEvidence(
                ByteString.Parse("0213b0006543d4ab6e79f49559fbfb18e9d73596d63f39e2f12ebc2c9d51e2eb06"),
                ByteString.Parse("304402200c7fba6b623efd7e52731a11e6d7b99c2ae752c0f950b7a444ef7fb80162498c02202b01c74a4a04fb120860494de09bd6848f088927a7b07e3c3925b3894c8c89d4"));

            ByteString result = await validator.PostTransaction(mutation, new[] { signature });

            Assert.Equal(32, result.Value.Count);
        }

        [Fact]
        public async Task Validate_InvalidSignature()
        {
            TransactionValidator validator = CreateValidator(defaultAccounts);
            ByteString mutation = CreateMutation("http://root/");

            SignatureEvidence signature = new SignatureEvidence(
                ByteString.Parse("0013b0006543d4ab6e79f49559fbfb18e9d73596d63f39e2f12ebc2c9d51e2eb06"),
                ByteString.Parse("304402200c7fba6b623efd7e52731a11e6d7b99c2ae752c0f950b7a444ef7fb80162498c02202b01c74a4a04fb120860494de09bd6848f088927a7b07e3c3925b3894c8c89d4"));

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new[] { signature }));
            Assert.Equal("InvalidSignature", exception.Reason);
        }

        private ByteString CreateMutation(string @namespace)
        {
            Mutation mutation = new Mutation(
                new ByteString(Encoding.UTF8.GetBytes(@namespace)),
                new Record[]
                {
                    new Record(
                        new AccountKey(LedgerPath.Parse("/account/1/"), LedgerPath.Parse("/a/")).Key.ToBinary(),
                        new ByteString(BitConverter.GetBytes(100L).Reverse()),
                        ByteString.Empty),
                    new Record(
                        new AccountKey(LedgerPath.Parse("/account/2/"), LedgerPath.Parse("/a/")).Key.ToBinary(),
                        new ByteString(BitConverter.GetBytes(100L).Reverse()),
                        ByteString.Empty),
                },
                ByteString.Empty);

            return new ByteString(MessageSerializer.SerializeMutation(mutation));
        }

        private TransactionValidator CreateValidator(IDictionary<string, long> accounts)
        {
            return new TransactionValidator(
                new TestStore(accounts, false),
                new TestValidator(false),
                "http://root/");
        }

        private class TestValidator : IMutationValidator
        {
            private readonly bool exception;

            public TestValidator(bool exception)
            {
                this.exception = exception;
            }

            public Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
            {
                if (exception)
                    throw new TransactionInvalidException("Test");
                else
                    return Task.FromResult(true);
            }
        }

        private class TestStore : IStorageEngine
        {
            private readonly IDictionary<string, long> accounts;
            private readonly bool exception;

            public TestStore(IDictionary<string, long> accounts, bool exception)
            {
                this.accounts = accounts;
                this.exception = exception;
            }

            public Task Initialize()
            {
                throw new NotImplementedException();
            }

            public Task AddTransactions(IEnumerable<ByteString> transactions)
            {
                if (this.exception)
                    throw new ConcurrentMutationException(new Record(ByteString.Empty, ByteString.Empty, ByteString.Empty));
                else
                    return Task.FromResult(0);
            }

            public Task<ByteString> GetLastTransaction()
            {
                throw new NotImplementedException();
            }

            public Task<IReadOnlyList<Record>> GetRecords(IEnumerable<ByteString> keys)
            {
                return Task.FromResult<IReadOnlyList<Record>>(keys.Select(key =>
                {
                    RecordKey recordKey = RecordKey.Parse(key);
                    return new Record(
                        key,
                        new ByteString(BitConverter.GetBytes(this.accounts[recordKey.Path.FullPath]).Reverse()),
                        ByteString.Empty);
                })
                .ToList());
            }

            public Task<IReadOnlyList<ByteString>> GetTransactions(ByteString from)
            {
                throw new NotImplementedException();
            }
        }
    }
}
