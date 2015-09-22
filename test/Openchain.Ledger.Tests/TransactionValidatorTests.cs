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
        [Fact]
        public async Task PostTransaction_Success()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>()
            {
                ["/account/1/"] = 90,
                ["/account/2/"] = 110,
            };

            TransactionValidator validator = CreateValidator(accounts);
            ByteString mutation = CreateMutation("http://root/");

            ByteString result = await validator.PostTransaction(mutation, new SignatureEvidence[0]);

            Assert.Equal(32, result.Value.Count);
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

            await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
        }

        [Fact]
        public async Task PostTransaction_InvalidNamespace()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>()
            {
                ["/account/1/"] = 90,
                ["/account/2/"] = 110,
            };

            TransactionValidator validator = CreateValidator(accounts);
            ByteString mutation = CreateMutation("http://wrong-root/");

            await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
        }

        [Fact]
        public async Task PostTransaction_ConcurrencyException()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>()
            {
                ["/account/1/"] = 90,
                ["/account/2/"] = 110,
            };

            TransactionValidator validator = new TransactionValidator(
                new TestStore(accounts, true),
                new TestValidator(false),
                "http://root/");

            ByteString mutation = CreateMutation("http://root/");

            await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
        }

        [Fact]
        public async Task PostTransaction_ValidationException()
        {
            Dictionary<string, long> accounts = new Dictionary<string, long>()
            {
                ["/account/1/"] = 90,
                ["/account/2/"] = 110,
            };

            TransactionValidator validator = new TransactionValidator(
                new TestStore(accounts, false),
                new TestValidator(true),
                "http://root/");

            ByteString mutation = CreateMutation("http://root/");

            await Assert.ThrowsAsync<TransactionInvalidException>(
                () => validator.PostTransaction(mutation, new SignatureEvidence[0]));
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

        private class TestStore : ITransactionStore
        {
            private readonly IDictionary<string, long> accounts;
            private readonly bool exception;

            public TestStore(IDictionary<string, long> accounts, bool exception)
            {
                this.accounts = accounts;
                this.exception = exception;
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

            public Task<IList<Record>> GetRecords(IEnumerable<ByteString> keys)
            {
                return Task.FromResult<IList<Record>>(keys.Select(key =>
                {
                    RecordKey recordKey = RecordKey.Parse(key);
                    return new Record(
                        key,
                        new ByteString(BitConverter.GetBytes(this.accounts[recordKey.Path.FullPath]).Reverse()),
                        ByteString.Empty);
                })
                .ToList());
            }

            public IObservable<ByteString> GetTransactionStream(ByteString from)
            {
                throw new NotImplementedException();
            }
        }
    }
}
