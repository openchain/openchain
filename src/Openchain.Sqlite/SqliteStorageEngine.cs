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

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.Sqlite
{
    public class SqliteStorageEngine : SqliteBase, IStorageEngine
    {
        public SqliteStorageEngine(string filename)
            : base(filename)
        { }

        #region Initialize

        public async Task Initialize()
        {
            await Connection.OpenAsync();
        }

        #endregion

        #region AddTransactions

        public async Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            using (SqliteTransaction context = Connection.BeginTransaction(IsolationLevel.Serializable))
            {
                foreach (ByteString rawTransaction in transactions)
                {
                    byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);

                    byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                    await UpdateAccounts(mutation, mutationHash);

                    IReadOnlyList<long> rowId = await ExecuteAsync(@"
                            INSERT INTO Transactions
                            (Hash, MutationHash, RawData)
                            VALUES (@hash, @mutationHash, @rawData);

                            SELECT last_insert_rowid();",
                        reader => (long)reader.GetValue(0),
                        new Dictionary<string, object>()
                        {
                            ["@hash"] = transactionHash,
                            ["@mutationHash"] = mutationHash,
                            ["@rawData"] = rawTransactionBuffer
                        });

                    await AddTransaction(rowId[0], mutationHash, mutation);
                }

                context.Commit();
            }
        }

        protected virtual Task AddTransaction(long transactionId, byte[] transactionHash, Mutation mutation)
        {
            return Task.FromResult(0);
        }

        private async Task UpdateAccounts(Mutation mutation, byte[] transactionHash)
        {
            foreach (Record record in mutation.Records)
            {
                if (record.Value == null)
                {
                    // Read the record and make sure it corresponds to the one supplied
                    IReadOnlyList<byte[]> versions = await ExecuteAsync(@"
                            SELECT  Version
                            FROM    Records
                            WHERE   Key = @key",
                        reader => (byte[])reader.GetValue(0),
                        new Dictionary<string, object>()
                        {
                            ["@key"] = record.Key.ToByteArray()
                        });

                    if (versions.Count == 0)
                    {
                        if (!record.Version.Equals(ByteString.Empty))
                            throw new ConcurrentMutationException(record);
                    }
                    else
                    {
                        if (!new ByteString(versions[0]).Equals(record.Version))
                            throw new ConcurrentMutationException(record);
                    }
                }
                else
                {
                    if (!record.Version.Equals(ByteString.Empty))
                    {
                        // Update existing account
                        int count = await ExecuteAsync(@"
                                UPDATE  Records
                                SET     Value = @value, Version = @version
                                WHERE   Key = @key AND Version = @currentVersion",
                            new Dictionary<string, object>()
                            {
                                ["@key"] = record.Key.ToByteArray(),
                                ["@value"] = record.Value.ToByteArray(),
                                ["@version"] = transactionHash,
                                ["@currentVersion"] = record.Version.ToByteArray()
                            });

                        if (count == 0)
                            throw new ConcurrentMutationException(record);
                    }
                    else
                    {
                        // Create a new record
                        try
                        {
                            await ExecuteAsync(@"
                                    INSERT INTO Records
                                    (Key, Value, Version)
                                    VALUES (@key, @value, @version)",
                                new Dictionary<string, object>()
                                {
                                    ["@key"] = record.Key.ToByteArray(),
                                    ["@value"] = record.Value.ToByteArray(),
                                    ["@version"] = transactionHash
                                });
                        }
                        catch (SqliteException exception) when (exception.Message.Contains("UNIQUE constraint failed: Records.Key"))
                        {
                            throw new ConcurrentMutationException(record);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetRecords

        public async Task<IReadOnlyList<Record>> GetRecords(IEnumerable<ByteString> keys)
        {
            Dictionary<ByteString, Record> result = new Dictionary<ByteString, Record>();

            foreach (ByteString key in keys)
            {
                SqliteCommand query = Connection.CreateCommand();
                query.CommandText = @"
                    SELECT  Value, Version
                    FROM    Records
                    WHERE   Key = @key";

                query.Parameters.AddWithValue("@key", key.ToByteArray());

                using (DbDataReader reader = await query.ExecuteReaderAsync())
                {
                    bool exists = await reader.ReadAsync();

                    if (exists)
                    {
                        result[key] = new Record(
                            key,
                            reader.GetValue(0) == null ? ByteString.Empty : new ByteString((byte[])reader.GetValue(0)),
                            new ByteString((byte[])reader.GetValue(1)));
                    }
                    else
                    {
                        result[key] = new Record(key, ByteString.Empty, ByteString.Empty);
                    }
                }
            }

            return result.Values.ToList().AsReadOnly();
        }

        #endregion

        #region GetLastTransaction

        public async Task<ByteString> GetLastTransaction()
        {
            IEnumerable<ByteString> transactions = await ExecuteAsync(@"
                    SELECT  Hash
                    FROM    Transactions
                    ORDER BY Id DESC
                    LIMIT 1",
                reader => new ByteString((byte[])reader.GetValue(0)),
                new Dictionary<string, object>());

            return transactions.FirstOrDefault() ?? ByteString.Empty;
        }

        #endregion

        #region GetTransactions

        public async Task<IReadOnlyList<ByteString>> GetTransactions(ByteString from)
        {
            Func<DbDataReader, ByteString> selector = reader => new ByteString((byte[])reader.GetValue(0));
            if (from != null)
            {
                return await ExecuteAsync(@"
                        SELECT  RawData
                        FROM    Transactions
                        WHERE   Id > (SELECT Id FROM Transactions WHERE Hash = @hash)
                        ORDER BY Id ASC",
                    selector,
                    new Dictionary<string, object>()
                    {
                        ["@hash"] = from.ToByteArray()
                    });
            }
            else
            {
                return await ExecuteAsync(@"
                        SELECT  RawData
                        FROM    Transactions
                        ORDER BY Id ASC",
                    selector,
                    new Dictionary<string, object>());
            }
        }

        #endregion
    }
}
