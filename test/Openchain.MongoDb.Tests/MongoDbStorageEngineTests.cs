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

using Openchain.Tests;
using System;
using MongoDB.Driver;

namespace Openchain.MongoDb.Tests
{
    public class MongoDbStorageEngineTests : BaseStorageEngineTests
    {
        public MongoDbStorageEngineTests()
        {
            var store = new MongoDbStorageEngine(
                            new MongoDbStorageEngineConfiguration {
                                ConnectionString="mongodb://localhost",
                                Database="openchaintest",
                                ReadLoopDelay=TimeSpan.FromMilliseconds(50),
                                ReadRetryCount=10,
                                StaleTransactionDelay=TimeSpan.FromMinutes(10),
                                RunRollbackThread=false                              
                            }, null);
            store.RecordCollection.DeleteMany(x => true);
            store.TransactionCollection.DeleteMany(x => true);
            store.PendingTransactionCollection.DeleteMany(x => true);

            this.Store = store;
        }
    }
}
