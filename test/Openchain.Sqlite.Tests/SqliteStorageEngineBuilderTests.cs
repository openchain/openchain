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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Openchain.Sqlite.Tests
{
    public class SqliteStorageEngineBuilderTests
    {
        private readonly IConfigurationSection configuration =
            new ConfigurationRoot(new[] { new MemoryConfigurationProvider(new MemoryConfigurationSource() { InitialData = new Dictionary<string, string>() { ["config:path"] = ":memory:" } }) })
            .GetSection("config");

        [Fact]
        public void Name_Success()
        {
            Assert.Equal("SQLite", new SqliteStorageEngineBuilder().Name);
        }

        [Fact]
        public async Task Build_Success()
        {
            SqliteStorageEngineBuilder builder = new SqliteStorageEngineBuilder();

            await builder.Initialize(new ServiceCollection().BuildServiceProvider(), configuration);

            SqliteLedger ledger = builder.Build(null);

            Assert.NotNull(ledger);
        }

        [Fact]
        public async Task InitializeTables_CallTwice()
        {
            SqliteStorageEngineBuilder builder = new SqliteStorageEngineBuilder();

            await builder.Initialize(new ServiceCollection().BuildServiceProvider(), configuration);

            SqliteLedger ledger = builder.Build(null);

            await SqliteStorageEngineBuilder.InitializeTables(ledger.Connection);
            await SqliteStorageEngineBuilder.InitializeTables(ledger.Connection);

            Assert.Equal(ConnectionState.Open, ledger.Connection.State);
        }

        [Fact]
        public void GetPathOrDefault_Success()
        {
            IServiceProvider services = new ServiceCollection().AddSingleton<IHostingEnvironment>(new TestHostingEnvironment()).BuildServiceProvider();

            string result = SqliteStorageEngineBuilder.GetPathOrDefault(services, "data.db");

            Assert.Equal(@"\path\data\data.db", result);
        }

        #region Test classes

        private class TestHostingEnvironment : IHostingEnvironment
        {
            public string ApplicationName { get; set; } = "";

            public IFileProvider ContentRootFileProvider { get; set; } = new TestFileProvider(@"\path\");

            public string ContentRootPath { get; set; } = @"\path";

            public string EnvironmentName { get; set; } = "test";

            public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

            public string WebRootPath { get; set; } = @"\path";
        }

        private class TestFileProvider : IFileProvider
        {
            private readonly string root;

            public TestFileProvider(string root)
            {
                this.root = root;
            }

            public IDirectoryContents GetDirectoryContents(string subpath) => null;

            public IFileInfo GetFileInfo(string subpath) => new TestFileInfo(root + subpath) { PhysicalPath = root + subpath };

            public IChangeToken Watch(string filter) => null;

            IChangeToken IFileProvider.Watch(string filter)
            {
                throw new NotImplementedException();
            }
        }

        public class TestFileInfo : IFileInfo
        {
            public TestFileInfo(string name)
            {
                Name = name;
            }

            public bool Exists
            {
                get { return true; }
            }

            public bool IsDirectory { get; set; }

            public DateTimeOffset LastModified { get; set; }

            public long Length { get; set; }

            public string Name { get; }

            public string PhysicalPath { get; set; }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
