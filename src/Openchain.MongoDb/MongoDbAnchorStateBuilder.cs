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
using Microsoft.Extensions.Configuration;
using Openchain.Infrastructure;
using MongoDB.Driver;

namespace Openchain.MongoDb
{
    public class MongoDbAnchorStateBuilder : IComponentBuilder<MongoDbAnchorState>
    {
        public string Name { get; } = "MongoDb";

        string connectionString
        {
            get;
            set;
        }

        string database
        {
            get;
            set;
        }


        public MongoDbAnchorState Build(IServiceProvider serviceProvider)
        {
            return new MongoDbAnchorState(connectionString,database);
        }

        public async Task Initialize(IServiceProvider serviceProvider, IConfigurationSection configuration)
        {
            connectionString = configuration["connection_string"];
            database = configuration["database"] ?? "openchain";
            using (var m = new MongoDbAnchorState(connectionString, database))
            {
                await m.AnchorStateCollection.Indexes.DropAllAsync();
                await m.AnchorStateCollection.Indexes.CreateOneAsync(Builders<MongoDbAnchorStateRecord>.IndexKeys.Ascending(x => x.Timestamp), new CreateIndexOptions { Background = true, Unique = true });
            }
        }

    }
}
