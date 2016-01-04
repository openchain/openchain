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
using System.Linq;
using System.Threading.Tasks;
using Openchain.Infrastructure;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Openchain.MongoDb
{
    public class MongoDbAnchorStateRecord
    {
        [BsonId]
        public byte[] Position { get; set; }
        public byte[] FullLedgerHash { get; set; }
        public long TransactionCount { get; set; }
        public BsonTimestamp Timestamp { get; set; } = new BsonTimestamp(0);

        [BsonExtraElements]
        public BsonDocument Extra { get; set; }
    }

    /// <summary>
    /// Persists information about the latest known anchor.
    /// </summary>
    public class MongoDbAnchorState : MongoDbBase, IAnchorState
    {
        internal IMongoCollection<MongoDbAnchorStateRecord> AnchorStateCollection
        {
            get;
            set;
        }

        public MongoDbAnchorState(string connectionString, string database)
            : base(connectionString,database)
        {
            AnchorStateCollection = Database.GetCollection<MongoDbAnchorStateRecord>("anchorstates");
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task Initialize()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
        }

        /// <summary>
        /// Gets the last known anchor.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<LedgerAnchor> GetLastAnchor()
        {
            var res = await AnchorStateCollection.Find(x => true).SortByDescending(x => x.Timestamp).FirstOrDefaultAsync();
            return res == null ? null : new LedgerAnchor(new ByteString(res.Position),new ByteString(res.FullLedgerHash), res.TransactionCount);
        }

        /// <summary>
        /// Marks the anchor as successfully recorded in the anchoring medium.
        /// </summary>
        /// <param name="anchor">The anchor to commit.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task CommitAnchor(LedgerAnchor anchor)
        {
            await AnchorStateCollection.InsertOneAsync(new MongoDbAnchorStateRecord
            {
                Position = anchor.Position.ToByteArray(),
                FullLedgerHash = anchor.FullStoreHash.ToByteArray(),
                TransactionCount = anchor.TransactionCount
            });
        }
    }
}
