﻿// Copyright 2015 Coinprism, Inc.
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
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Openchain.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Openchain.Sqlite
{
    public class SqliteStorageEngineBuilder : IComponentBuilder<SqliteLedger>
    {
        private static readonly string columnAlreadyExistsMessage = "SQLite Error 1: 'duplicate column name: Name'.";
        private string filename;

        public string Name { get; } = "SQLite";

        public SqliteLedger Build(IServiceProvider serviceProvider)
        {
            return new SqliteLedger(filename);
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            filename = GetPathOrDefault(serviceProvider, configuration["path"]);

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
                    CREATE TABLE IF NOT EXISTS Transactions
                    (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Hash BLOB UNIQUE,
                        MutationHash BLOB UNIQUE,
                        RawData BLOB
                    );

                    CREATE TABLE IF NOT EXISTS Records
                    (
                        Key BLOB PRIMARY KEY,
                        Value BLOB,
                        Version BLOB
                    );";

            await command.ExecuteNonQueryAsync();

            try
            {
                command = connection.CreateCommand();
                command.CommandText = @"
                        ALTER TABLE Records ADD COLUMN Name TEXT;
                        ALTER TABLE Records ADD COLUMN Type INTEGER;";

                await command.ExecuteNonQueryAsync();
            }
            catch (SqliteException exception) when (exception.Message == columnAlreadyExistsMessage)
            { }

            // Index of transactions affecting a given record
            command = connection.CreateCommand();
            command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS RecordMutations
                    (
                        RecordKey BLOB,
                        TransactionId INTEGER,
                        MutationHash BLOB,
                        PRIMARY KEY (RecordKey, TransactionId)
                    );";

            await command.ExecuteNonQueryAsync();
        }

        public static string GetPathOrDefault(IServiceProvider serviceProvider, string path)
        {
            if (path != ":memory:")
            {
                IHostingEnvironment environment = serviceProvider.GetRequiredService<IHostingEnvironment>();
                return environment.ContentRootFileProvider.GetFileInfo(Path.Combine("data", path)).PhysicalPath;
            }
            else
            {
                return path;
            }
        }
    }
}
