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

namespace Openchain.Infrastructure
{
    public static class StorageEngineExtensions
    {
        public static async Task<Record> GetRecord(this IStorageEngine store, RecordKey key)
        {
            IReadOnlyList<Record> result = await store.GetRecords(new[] { key.ToBinary() });
            return result[0];
        }

        public static async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccounts(this IStorageEngine store, IEnumerable<AccountKey> accounts)
        {
            IReadOnlyList<Record> records = await store.GetRecords(accounts.Select(account => account.Key.ToBinary()));

            return records.Select(record => AccountStatus.FromRecord(RecordKey.Parse(record.Key), record)).ToDictionary(account => account.AccountKey, account => account);
        }
    }
}
