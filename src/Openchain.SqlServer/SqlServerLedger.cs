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
using System.Collections.Generic;
using System.Threading.Tasks;
using Openchain.Ledger;

namespace Openchain.SqlServer
{
    public class SqlServerLedger : SqlServerStorageEngine, ILedgerQueries, ILedgerIndexes
    {
        private readonly int instanceId;

        public SqlServerLedger(string connectionString, int instanceId, TimeSpan commandTimeout)
            : base(connectionString, instanceId, commandTimeout)
        {
            this.instanceId = instanceId;
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix)
        {
            byte[] from = prefix.ToByteArray();
            byte[] to = prefix.ToByteArray();
            to[to.Length - 1]++;

            return await ExecuteQuery<Record>(
                "EXEC [Openchain].[GetRecordRange] @instance, @from, @to;",
                reader => new Record(new ByteString((byte[])reader[0]), new ByteString((byte[])reader[1]), new ByteString((byte[])reader[2])),
                new Dictionary<string, object>()
                {
                    ["instance"] = this.instanceId,
                    ["from"] = from,
                    ["to"] = to
                });
        }

        public async Task<IReadOnlyList<ByteString>> GetRecordMutations(ByteString recordKey)
        {
            return await ExecuteQuery<ByteString>(
                "EXEC [Openchain].[GetRecordMutations] @instance, @recordKey;",
                reader => new ByteString((byte[])reader[0]),
                new Dictionary<string, object>()
                {
                    ["instance"] = this.instanceId,
                    ["recordKey"] = recordKey.ToByteArray()
                });
        }

        public async Task<ByteString> GetTransaction(ByteString mutationHash)
        {
            IReadOnlyList<ByteString> result = await ExecuteQuery<ByteString>(
                "EXEC [Openchain].[GetTransaction] @instance, @mutationHash;",
                reader => new ByteString((byte[])reader[0]),
                new Dictionary<string, object>()
                {
                    ["instance"] = this.instanceId,
                    ["mutationHash"] = mutationHash.ToByteArray()
                });

            if (result.Count > 0)
                return result[0];
            else
                return null;
        }

        public async Task<IReadOnlyList<Record>> GetAllRecords(RecordType type, string name)
        {
            return await ExecuteQuery<Record>(
                "EXEC [Openchain].[GetAllRecords] @instance, @recordType, @recordName;",
                reader => new Record(new ByteString((byte[])reader[0]), new ByteString((byte[])reader[1]), new ByteString((byte[])reader[2])),
                new Dictionary<string, object>()
                {
                    ["instance"] = this.instanceId,
                    ["recordType"] = (byte)type,
                    ["recordName"] = name
                });
        }

        protected override RecordKey ParseRecordKey(ByteString key)
        {
            return RecordKey.Parse(key);
        }
    }
}
