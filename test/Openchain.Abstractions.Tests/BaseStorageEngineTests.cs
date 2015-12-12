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
using System.Threading.Tasks;
using Xunit;

namespace Openchain.Sqlite.Tests
{
    public abstract class BaseStorageEngineTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        protected IStorageEngine Store { get; set; }

        [Fact]
        public async Task AddTransaction_InsertSuccess()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[2], null, ByteString.Empty),
                new Record(binaryData[4], ByteString.Empty, ByteString.Empty));

            IList<Record> records1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.Store.GetRecords(new[] { binaryData[2] });
            IList<Record> records3 = await this.Store.GetRecords(new[] { binaryData[4] });
            IList<Record> records4 = await this.Store.GetRecords(new[] { binaryData[6] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[2], ByteString.Empty, ByteString.Empty);
            Assert.Equal(1, records3.Count);
            AssertRecord(records3[0], binaryData[4], ByteString.Empty, mutationHash);
            Assert.Equal(1, records4.Count);
            AssertRecord(records4[0], binaryData[6], ByteString.Empty, ByteString.Empty);
        }

        [Fact]
        public async Task AddTransaction_UpdateSuccess()
        {
            ByteString mutationHash1 = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty));

            ByteString mutationHash2 = await AddTransaction(
                new Record(binaryData[3], binaryData[4], ByteString.Empty));

            ByteString mutationHash3 = await AddTransaction(
                new Record(binaryData[0], binaryData[2], mutationHash1),
                new Record(binaryData[3], null, mutationHash2));

            IList<Record> records1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.Store.GetRecords(new[] { binaryData[3] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[2], mutationHash3);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[3], binaryData[4], mutationHash2);
        }

        [Fact]
        public async Task AddTransaction_InsertError()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty));

            ConcurrentMutationException exception1 = await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[0], binaryData[2], ByteString.Empty)));

            ConcurrentMutationException exception2 = await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[4], null, binaryData[5])));

            IList<Record> records1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.Store.GetRecords(new[] { binaryData[4] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            AssertRecord(exception1.FailedMutation, binaryData[0], binaryData[2], ByteString.Empty);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[4], ByteString.Empty, ByteString.Empty);
            AssertRecord(exception2.FailedMutation, binaryData[4], null, binaryData[5]);
        }

        [Fact]
        public async Task AddTransaction_UpdateError()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[4], binaryData[5], ByteString.Empty));

            ConcurrentMutationException exception1 = await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[0], binaryData[2], binaryData[3])));

            ConcurrentMutationException exception2 = await Assert.ThrowsAsync<ConcurrentMutationException>(() => AddTransaction(
                new Record(binaryData[4], null, binaryData[6])));

            IList<Record> records1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> records2 = await this.Store.GetRecords(new[] { binaryData[4] });

            Assert.Equal(1, records1.Count);
            AssertRecord(records1[0], binaryData[0], binaryData[1], mutationHash);
            AssertRecord(exception1.FailedMutation, binaryData[0], binaryData[2], binaryData[3]);
            Assert.Equal(1, records2.Count);
            AssertRecord(records2[0], binaryData[4], binaryData[5], mutationHash);
            AssertRecord(exception2.FailedMutation, binaryData[4], null, binaryData[6]);
        }


        [Fact]
        public async Task AddTransaction_MultipleTransactionsSuccess()
        {
            IList<Record> records1 = new Record[]
            {
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[2], binaryData[3], ByteString.Empty),
            };

            ByteString mutation1 = new ByteString(MessageSerializer.SerializeMutation(new Mutation(ByteString.Empty, records1, ByteString.Empty)));
            ByteString mutationHash1 = new ByteString(MessageSerializer.ComputeHash(mutation1.ToByteArray()));

            IList<Record> records2 = new Record[]
            {
                new Record(binaryData[2], binaryData[5], mutationHash1),
                new Record(binaryData[6], binaryData[7], ByteString.Empty),
            };

            ByteString mutation2 = new ByteString(MessageSerializer.SerializeMutation(new Mutation(ByteString.Empty, records2, ByteString.Empty)));
            ByteString mutationHash2 = new ByteString(MessageSerializer.ComputeHash(mutation2.ToByteArray()));

            // Submit both transactions at once
            await this.Store.AddTransactions(new[]
            {
                new ByteString(MessageSerializer.SerializeTransaction(new Transaction(mutation1, new DateTime(), ByteString.Empty))),
                new ByteString(MessageSerializer.SerializeTransaction(new Transaction(mutation2, new DateTime(), ByteString.Empty)))
            });

            IList<Record> result1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> result2 = await this.Store.GetRecords(new[] { binaryData[2] });
            IList<Record> result3 = await this.Store.GetRecords(new[] { binaryData[6] });

            AssertRecord(result1[0], binaryData[0], binaryData[1], mutationHash1);
            AssertRecord(result2[0], binaryData[2], binaryData[5], mutationHash2);
            AssertRecord(result3[0], binaryData[6], binaryData[7], mutationHash2);
        }

        [Fact]
        public async Task AddTransaction_MultipleTransactionsError()
        {
            IList<Record> records1 = new Record[]
            {
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[2], binaryData[3], ByteString.Empty),
            };

            ByteString mutation1 = new ByteString(MessageSerializer.SerializeMutation(new Mutation(ByteString.Empty, records1, ByteString.Empty)));

            IList<Record> records2 = new Record[]
            {
                new Record(binaryData[2], binaryData[5], ByteString.Empty),
                new Record(binaryData[6], binaryData[7], ByteString.Empty),
            };

            ByteString mutation2 = new ByteString(MessageSerializer.SerializeMutation(new Mutation(ByteString.Empty, records2, ByteString.Empty)));

            // Submit both transactions at once
            ConcurrentMutationException exception = await Assert.ThrowsAsync<ConcurrentMutationException>(() => this.Store.AddTransactions(new[]
            {
                new ByteString(MessageSerializer.SerializeTransaction(new Transaction(mutation1, new DateTime(), ByteString.Empty))),
                new ByteString(MessageSerializer.SerializeTransaction(new Transaction(mutation2, new DateTime(), ByteString.Empty)))
            }));

            IList<Record> result1 = await this.Store.GetRecords(new[] { binaryData[0] });
            IList<Record> result2 = await this.Store.GetRecords(new[] { binaryData[2] });
            IList<Record> result3 = await this.Store.GetRecords(new[] { binaryData[6] });

            AssertRecord(exception.FailedMutation, binaryData[2], binaryData[5], ByteString.Empty);
            AssertRecord(result1[0], binaryData[0], ByteString.Empty, ByteString.Empty);
            AssertRecord(result2[0], binaryData[2], ByteString.Empty, ByteString.Empty);
            AssertRecord(result3[0], binaryData[6], ByteString.Empty, ByteString.Empty);
        }

        [Fact]
        public async Task GetRecords_MultipleRecords()
        {
            ByteString mutationHash = await AddTransaction(
                new Record(binaryData[0], binaryData[1], ByteString.Empty),
                new Record(binaryData[2], binaryData[3], ByteString.Empty));

            IList<Record> records = await this.Store.GetRecords(new[] { binaryData[0], binaryData[2], binaryData[4] });

            Assert.Equal(3, records.Count);
            AssertRecord(records.First(record => record.Key.Equals(binaryData[0])), binaryData[0], binaryData[1], mutationHash);
            AssertRecord(records.First(record => record.Key.Equals(binaryData[2])), binaryData[2], binaryData[3], mutationHash);
            AssertRecord(records.First(record => record.Key.Equals(binaryData[4])), binaryData[4], ByteString.Empty, ByteString.Empty);
        }

        [Fact]
        public async Task GetLastTransaction_Success()
        {
            await AddTransaction(new Record(binaryData[0], binaryData[1], ByteString.Empty));
            await AddTransaction(new Record(binaryData[2], binaryData[3], ByteString.Empty));

            ByteString lastTransaction = await this.Store.GetLastTransaction();

            Assert.Equal(ByteString.Parse("6e55f997a10bf4f9db8b2e1341c1a402418be3e7496ecb77f364fefecaeaeb43"), lastTransaction);
        }

        [Fact]
        public async Task GetLastTransaction_NoTransaction()
        {
            ByteString lastTransaction = await this.Store.GetLastTransaction();

            Assert.Equal(ByteString.Empty, lastTransaction);
        }

        [Fact]
        public async Task GetTransactionStream_FromStart()
        {
            TestObserver observer = new TestObserver() { ExpectedValueCount = 2 };

            ByteString mutation1 = await AddTransaction(new Record(binaryData[0], binaryData[1], ByteString.Empty));
            ByteString mutation2 = await AddTransaction(new Record(binaryData[2], binaryData[3], ByteString.Empty));

            IObservable<ByteString> stream = this.Store.GetTransactionStream(null);
            using (stream.Subscribe(observer))
                await observer.Completed.Task;

            await observer.Disposed.Task;

            Assert.False(observer.Fail);
            Assert.Equal(2, observer.Values.Count);
            Assert.Equal(mutation1, GetMutationHash(observer.Values[0]));
            Assert.Equal(mutation2, GetMutationHash(observer.Values[1]));
        }

        [Fact]
        public async Task GetTransactionStream_Resume()
        {
            TestObserver observer = new TestObserver() { ExpectedValueCount = 1 };

            await AddTransaction(new Record(binaryData[0], binaryData[1], ByteString.Empty));
            ByteString resumeToken = await this.Store.GetLastTransaction();
            ByteString mutation2 = await AddTransaction(new Record(binaryData[2], binaryData[3], ByteString.Empty));

            IObservable<ByteString> stream = this.Store.GetTransactionStream(resumeToken);
            using (stream.Subscribe(observer))
                await observer.Completed.Task;

            await observer.Disposed.Task;

            Assert.False(observer.Fail);
            Assert.Equal(1, observer.Values.Count);
            Assert.Equal(mutation2, GetMutationHash(observer.Values[0]));
        }

        [Fact]
        public async Task GetTransactionStream_Error()
        {
            // Create a new storage engine with no table so that any query will throw an exception
            this.Store = CreateNewEngine();
            TestObserver observer = new TestObserver() { ExpectedValueCount = 1 };

            IObservable<ByteString> stream = this.Store.GetTransactionStream(null);
            using (stream.Subscribe(observer))
                await observer.Completed.Task;

            await observer.Disposed.Task;

            Assert.True(observer.Fail);
            Assert.Equal(0, observer.Values.Count);
        }

        protected abstract IStorageEngine CreateNewEngine();

        private async Task<ByteString> AddTransaction(params Record[] records)
        {
            Mutation mutation = new Mutation(ByteString.Parse("0123"), records, ByteString.Parse("4567"));
            ByteString serializedMutation = new ByteString(MessageSerializer.SerializeMutation(mutation));
            Transaction transaction = new Transaction(
                serializedMutation,
                new DateTime(1, 2, 3, 4, 5, 6),
                ByteString.Parse("abcdef"));

            await this.Store.AddTransactions(new[] { new ByteString(MessageSerializer.SerializeTransaction(transaction)) });

            return new ByteString(MessageSerializer.ComputeHash(serializedMutation.ToByteArray()));
        }

        private static void AssertRecord(Record record, ByteString key, ByteString value, ByteString version)
        {
            Assert.Equal(key, record.Key);
            Assert.Equal(value, record.Value);
            Assert.Equal(version, record.Version);
        }

        private static ByteString GetMutationHash(ByteString transaction)
        {
            return new ByteString(
                MessageSerializer.ComputeHash(MessageSerializer.DeserializeTransaction(transaction).Mutation.ToByteArray()));
        }

        private class TestObserver : IObserver<ByteString>
        {
            public int ExpectedValueCount { get; set; }

            public TaskCompletionSource<int> Completed { get; } = new TaskCompletionSource<int>();

            public TaskCompletionSource<int> Disposed { get; } = new TaskCompletionSource<int>();

            public IList<ByteString> Values { get; } = new List<ByteString>();

            public bool Fail { get; set; }

            public void OnCompleted() => Disposed.SetResult(0);

            public void OnError(Exception error)
            {
                Fail = true;
                this.Completed.SetResult(0);
                Disposed.SetResult(0);
            }

            public void OnNext(ByteString value)
            {
                Values.Add(value);

                if (Values.Count == ExpectedValueCount)
                    this.Completed.SetResult(0);
            }
        }
    }
}
