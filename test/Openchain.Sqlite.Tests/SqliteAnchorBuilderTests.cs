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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Openchain.Ledger;
using Xunit;

namespace Openchain.Sqlite.Tests
{
    public class SqliteAnchorBuilderTests
    {
        private readonly SqliteStorageEngine storageEngine;
        private readonly SqliteAnchorBuilder anchorBuilder;

        public SqliteAnchorBuilderTests()
        {
            this.anchorBuilder = new SqliteAnchorBuilder(":memory:");
            this.anchorBuilder.Initialize().Wait();
            this.storageEngine = new SqliteStorageEngine(":memory:");
            this.storageEngine.Initialize().Wait();
            SqliteStorageEngineBuilder.InitializeTables(this.storageEngine.Connection).Wait();
        }

        [Fact]
        public async Task CreateAnchor_ZeroTransaction()
        {
            LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor(storageEngine);

            Assert.Null(anchor);
        }

        [Fact]
        public async Task CreateAnchor_OneTransaction()
        {
            ByteString hash = await AddRecord("key1");

            LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor(storageEngine);

            Assert.Equal(1, anchor.TransactionCount);
            Assert.Equal(hash, anchor.Position);
            Assert.Equal(CombineHashes(new ByteString(new byte[32]), hash), anchor.FullStoreHash);
        }

        [Fact]
        public async Task CreateAnchor_TwoTransactions()
        {
            ByteString hash1 = await AddRecord("key1");
            ByteString hash2 = await AddRecord("key2");
            ByteString expectedCumulativeHash = CombineHashes(CombineHashes(new ByteString(new byte[32]), hash1), hash2);

            LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor(storageEngine);

            Assert.Equal(2, anchor.TransactionCount);
            Assert.Equal(hash2, anchor.Position);
            Assert.Equal(expectedCumulativeHash, anchor.FullStoreHash);
        }

        [Fact]
        public async Task CreateAnchor_OnePlusOneTransaction()
        {
            ByteString hash1 = await AddRecord("key1");
            await this.anchorBuilder.CommitAnchor(await this.anchorBuilder.CreateAnchor(storageEngine));
            ByteString hash2 = await AddRecord("key2");
            ByteString expectedCumulativeHash = CombineHashes(CombineHashes(new ByteString(new byte[32]), hash1), hash2);

            LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor(storageEngine);

            Assert.Equal(2, anchor.TransactionCount);
            Assert.Equal(hash2, anchor.Position);
            Assert.Equal(expectedCumulativeHash, anchor.FullStoreHash);
        }

        [Fact]
        public async Task CreateAnchor_RepeatSameAnchor()
        {
            ByteString hash = await AddRecord("key1");
            await this.anchorBuilder.CommitAnchor(await this.anchorBuilder.CreateAnchor(storageEngine));

            LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor(storageEngine);

            Assert.Null(anchor);
        }

        private async Task<ByteString> AddRecord(string key)
        {
            Mutation mutation = new Mutation(
                ByteString.Empty,
                new Record[] { new Record(new ByteString(Encoding.UTF8.GetBytes(key)), ByteString.Empty, ByteString.Empty) },
                ByteString.Empty);

            Transaction transaction = new Transaction(
                new ByteString(MessageSerializer.SerializeMutation(mutation)),
                new DateTime(),
                ByteString.Empty);

            await storageEngine.AddTransactions(new[] { new ByteString(MessageSerializer.SerializeTransaction(transaction)) });

            return new ByteString(MessageSerializer.ComputeHash(MessageSerializer.SerializeTransaction(transaction)));
        }

        private static ByteString CombineHashes(ByteString left, ByteString right)
        {
            using (SHA256 sha = SHA256.Create())
                return new ByteString(sha.ComputeHash(sha.ComputeHash(left.ToByteArray().Concat(right.ToByteArray()).ToArray())));
        }
    }
}
