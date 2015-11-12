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
using System.Text;
using System.Threading.Tasks;

namespace Openchain.Ledger
{
    public static class LedgerQueriesExtensions
    {
        public static async Task<IReadOnlyList<AccountStatus>> GetAccount(this ILedgerQueries queries, string account)
        {
            ByteString prefix = new ByteString(Encoding.UTF8.GetBytes(account + ":ACC:"));
            IReadOnlyList<Record> records = await queries.GetKeyStartingFrom(prefix);

            return records
                .Select(record => AccountStatus.FromRecord(RecordKey.Parse(record.Key), record))
                .ToList()
                .AsReadOnly();
        }

        public static async Task<IReadOnlyList<Record>> GetSubaccounts(this ILedgerQueries queries, string rootAccount)
        {
            ByteString prefix = new ByteString(Encoding.UTF8.GetBytes(rootAccount));
            return await queries.GetKeyStartingFrom(prefix);
        }

        public static async Task<Record> GetRecordVersion(this ILedgerQueries queries, ByteString key, ByteString version)
        {
            if (version.Value.Count == 0)
            {
                return new Record(key, ByteString.Empty, ByteString.Empty);
            }
            else
            {
                ByteString rawTransaction = await queries.GetTransaction(version);

                if (rawTransaction == null)
                {
                    return null;
                }
                else
                {
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                    Record result = mutation.Records.FirstOrDefault(record => record.Key.Equals(key) && record.Value != null);

                    if (result == null)
                        return null;
                    else
                        return result;
                }
            }
        }
    }
}
