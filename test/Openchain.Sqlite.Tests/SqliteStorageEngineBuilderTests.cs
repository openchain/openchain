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

using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace Openchain.Sqlite.Tests
{
    public class SqliteStorageEngineBuilderTests
    {
        [Fact]
        public void Name_Success()
        {
            Assert.Equal("SQLite", new SqliteStorageEngineBuilder().Name);
        }

        [Fact]
        public async Task Build_Success()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>() { ["path"] = ":memory:" };
            SqliteStorageEngineBuilder builder = new SqliteStorageEngineBuilder();

            await builder.Initialize(parameters);

            SqliteLedger ledger = builder.Build(null);

            Assert.NotNull(ledger);
        }

        [Fact]
        public async Task InitializeTables_CallTwice()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>() { ["path"] = ":memory:" };
            SqliteStorageEngineBuilder builder = new SqliteStorageEngineBuilder();

            await builder.Initialize(parameters);

            SqliteLedger ledger = builder.Build(null);

            await SqliteStorageEngineBuilder.InitializeTables(ledger.Connection);
            await SqliteStorageEngineBuilder.InitializeTables(ledger.Connection);

            Assert.Equal(ConnectionState.Open, ledger.Connection.State);
        }
    }
}
