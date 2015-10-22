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
using System.Collections.ObjectModel;
using System.Text;

namespace Openchain.Ledger.Validation
{
    public class ParsedMutation
    {
        public ParsedMutation(
            IList<AccountStatus> accountMutations,
            IList<KeyValuePair<RecordKey, ByteString>> dataRecords)
        {
            this.AccountMutations = new ReadOnlyCollection<AccountStatus>(accountMutations);
            this.DataRecords = new ReadOnlyCollection<KeyValuePair<RecordKey, ByteString>>(dataRecords);
        }

        public IReadOnlyList<AccountStatus> AccountMutations { get; }

        public IReadOnlyList<KeyValuePair<RecordKey, ByteString>> DataRecords { get; }

        public static ParsedMutation Parse(Mutation mutation)
        {
            List<AccountStatus> accountMutations = new List<AccountStatus>();
            List<KeyValuePair<RecordKey, ByteString>> dataRecords = new List<KeyValuePair<RecordKey, ByteString>>();

            foreach (Record record in mutation.Records)
            {
                // This is used for optimistic concurrency and does not participate in the validation
                if (record.Value == null)
                    continue;

                try
                {
                    RecordKey key = RecordKey.Parse(record.Key);
                    switch (key.RecordType)
                    {
                        case RecordType.Account:
                            accountMutations.Add(AccountStatus.FromRecord(key, record));
                            break;
                        case RecordType.Data:
                            dataRecords.Add(new KeyValuePair<RecordKey, ByteString>(key, record.Value));
                            break;
                    }
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "keyData")
                {
                    throw new TransactionInvalidException("NonCanonicalSerialization");
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "path")
                {
                    throw new TransactionInvalidException("InvalidPath");
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "recordType")
                {
                    throw new TransactionInvalidException("InvalidRecord");
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "record")
                {
                    throw new TransactionInvalidException("InvalidRecord");
                }
            }

            return new ParsedMutation(accountMutations, dataRecords);
        }
    }
}
