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
using System.Data.SqlClient;
using Openchain.Infrastructure.Tests;
using Xunit;

namespace Openchain.SqlServer.Tests
{
    [Collection("SQL Server Tests")]
    public class SqlServerLedgerTests : BaseLedgerTests
    {
        public SqlServerLedgerTests()
        {
            SqlServerLedger engine = new SqlServerLedger(ConfigurationManager.GetSetting("sql_connection_string"), 1, TimeSpan.FromSeconds(10));
            engine.Initialize().Wait();

            SqlCommand command = engine.Connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM [Openchain].[RecordMutations];
                DELETE FROM [Openchain].[Records];
                DELETE FROM [Openchain].[Transactions];
            ";

            command.ExecuteNonQuery();

            this.Engine = engine;
            this.Queries = engine;
            this.Indexes = engine;
        }
    }
}
