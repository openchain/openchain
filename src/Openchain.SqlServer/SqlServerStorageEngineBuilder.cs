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

using Microsoft.Extensions.Configuration;
using Openchain.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Openchain.SqlServer
{
    public class SqlServerStorageEngineBuilder : IComponentBuilder<SqlServerLedger>
    {
        private string connectionString;

        public string Name { get; } = "MSSQL";

        public SqlServerLedger Build(IServiceProvider serviceProvider)
        {
            return new SqlServerLedger(connectionString, 1, TimeSpan.FromSeconds(10));
        }

        public Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            this.connectionString = configuration["connection_string"];

            return Task.FromResult(0);
        }
    }
}
