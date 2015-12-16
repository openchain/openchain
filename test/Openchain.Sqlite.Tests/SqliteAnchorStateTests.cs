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

using System.Linq;
using System.Threading.Tasks;
using Openchain.Ledger;
using Xunit;

namespace Openchain.Sqlite.Tests
{
    public class SqliteAnchorStateTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        private readonly SqliteAnchorState anchorBuilder;

        public SqliteAnchorStateTests()
        {
            this.anchorBuilder = new SqliteAnchorState(":memory:");
            this.anchorBuilder.Initialize().Wait();
            SqliteAnchorStateBuilder.InitializeTables(this.anchorBuilder.Connection).Wait();
        }

        [Fact]
        public async Task GetLastAnchor_Success()
        {
            await this.anchorBuilder.CommitAnchor(new LedgerAnchor(binaryData[0], binaryData[1], 100));
            await this.anchorBuilder.CommitAnchor(new LedgerAnchor(binaryData[2], binaryData[3], 101));

            LedgerAnchor anchor = await this.anchorBuilder.GetLastAnchor();

            Assert.Equal(binaryData[2], anchor.Position);
            Assert.Equal(binaryData[3], anchor.FullStoreHash);
            Assert.Equal(101, anchor.TransactionCount);
        }

        [Fact]
        public async Task GetLastAnchor_NoAnchor()
        {
            LedgerAnchor anchor = await this.anchorBuilder.GetLastAnchor();

            Assert.Null(anchor);
        }
    }
}
