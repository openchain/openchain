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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Openchain.SqlServer.Tests
{
    public class SqlServerStorageEngineBuilderTests
    {
        private readonly IConfigurationSection configuration =
            new ConfigurationRoot(new[] { new MemoryConfigurationProvider(new Dictionary<string, string>() { ["config:connection_string"] = ConfigurationManager.GetSetting("sql_connection_string") }) })
            .GetSection("config");

        [Fact]
        public void Name_Success()
        {
            Assert.Equal("MSSQL", new SqlServerStorageEngineBuilder().Name);
        }

        [Fact]
        public async Task Build_Success()
        {
            SqlServerStorageEngineBuilder builder = new SqlServerStorageEngineBuilder();

            await builder.Initialize(new ServiceCollection().BuildServiceProvider(), configuration);

            SqlServerLedger ledger = builder.Build(null);

            Assert.NotNull(ledger);
        }
    }
}
