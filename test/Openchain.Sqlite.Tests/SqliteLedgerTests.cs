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

using Openchain.Infrastructure.Tests;

namespace Openchain.Sqlite.Tests
{
    public class SqliteLedgerTests : BaseLedgerTests
    {
        public SqliteLedgerTests()
        {
            SqliteLedger store = new SqliteLedger(":memory:");
            store.Initialize().Wait();
            SqliteStorageEngineBuilder.InitializeTables(store.Connection).Wait();

            this.Engine = store;
            this.Queries = store;
            this.Indexes = store;
        }
    }
}
