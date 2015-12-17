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
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Openchain.Ledger;

namespace Openchain.Sqlite
{
    public class SqliteAnchorStateBuilder : IComponentBuilder<SqliteAnchorState>
    {
        private string filename;

        public string Name { get; } = "SQLite";

        public SqliteAnchorState Build(IServiceProvider serviceProvider)
        {
            return new SqliteAnchorState(filename);
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            filename = SqliteStorageEngineBuilder.GetPathOrDefault(serviceProvider, configuration["path"]);

            using (SqliteConnection connection = new SqliteConnection(new SqliteConnectionStringBuilder() { DataSource = filename }.ToString()))
            {
                await InitializeTables(connection);
            }
        }

        public static async Task InitializeTables(SqliteConnection connection)
        {
            await connection.OpenAsync();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Anchors
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Position BLOB UNIQUE,
                    FullLedgerHash BLOB,
                    TransactionCount INT,
                    AnchorId BLOB
                );";

            await command.ExecuteNonQueryAsync();
        }
    }
}
