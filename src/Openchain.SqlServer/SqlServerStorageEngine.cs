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
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace Openchain.Sqlite
{
    public class SqlServerStorageEngine : IStorageEngine
    {
        private readonly int instanceId;
        private readonly TimeSpan commandTimeout;
        private readonly SqlMetaData[] metadata = new[]
        {
            new SqlMetaData("Key", SqlDbType.VarBinary, 512),
            new SqlMetaData("Value", SqlDbType.VarBinary, SqlMetaData.Max),
            new SqlMetaData("Version", SqlDbType.VarBinary, 32),
            new SqlMetaData("Name", SqlDbType.VarChar, 512),
            new SqlMetaData("Type", SqlDbType.TinyInt),
        };

        public SqlServerStorageEngine(string connectionString, int instanceId, TimeSpan commandTimeout)
        {
            this.Connection = new SqlConnection(connectionString);
            this.instanceId = instanceId;
            this.commandTimeout = commandTimeout;
        }

        protected SqlConnection Connection { get; }

        public Task OpenConnection()
        {
            return Connection.OpenAsync();
        }

        #region AddTransactions

        public async Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            using (SqlTransaction context = Connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                foreach (ByteString rawTransaction in transactions)
                {
                    byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);

                    byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                    await ExecuteQuery<long>(
                        "EXEC [Openchain].[AddTransaction] @instance, @transactionHash, @mutationHash, @rawData, @records;",
                        reader => (long)reader[0],
                        new Dictionary<string, object>()
                        {
                            ["instance"] = this.instanceId,
                            ["transactionHash"] = transactionHash,
                            ["mutationHash"] = mutationHash,
                            ["rawData"] = rawTransactionBuffer,
                            ["records"] = mutation.Records.Select(record =>
                            {
                                SqlDataRecord result = new SqlDataRecord(metadata);

                                result.SetBytes(0, 0, record.Key.ToByteArray(), 0, record.Key.Value.Count);

                                if (record.Value == null)
                                    result.SetDBNull(1);
                                else
                                    result.SetBytes(1, 0, record.Value.ToByteArray(), 0, record.Value.Value.Count);

                                result.SetBytes(2, 0, record.Version.ToByteArray(), 0, record.Version.Value.Count);
                                result.SetString(3, "");
                                result.SetByte(4, 0);
                                return result;
                            }).ToList()
                        },
                        context);
                }

                context.Commit();
            }
        }

        #endregion

        #region GetRecords

        public Task<IList<Record>> GetRecords(IEnumerable<ByteString> keys)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region GetLastTransaction

        public Task<ByteString> GetLastTransaction()
        {
            throw new NotSupportedException();
        }

        #endregion

        #region GetTransactionStream

        public IObservable<ByteString> GetTransactionStream(ByteString from)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Private Methods

        public async Task<IReadOnlyList<T>> ExecuteQuery<T>(string query, Func<SqlDataReader, T> readRecord, IDictionary<string, object> parameters, SqlTransaction transaction = null)
        {
            List<T> result = new List<T>();

            using (SqlCommand command = new SqlCommand(query, this.Connection))
            {
                command.Transaction = transaction;
                SetQueryParameters(parameters, command);
                using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default | CommandBehavior.SingleResult))
                {
                    while (await reader.ReadAsync())
                        result.Add(readRecord(reader));
                }
            }

            return result.AsReadOnly();
        }

        public async Task<int> ExecuteNonQuery(string query, IDictionary<string, object> parameters)
        {
            using (SqlCommand command = new SqlCommand(query, this.Connection))
            {
                SetQueryParameters(parameters, command);
                return await command.ExecuteNonQueryAsync();
            }
        }

        private void SetQueryParameters(IDictionary<string, object> parameters, SqlCommand command)
        {
            command.CommandTimeout = (int)commandTimeout.TotalSeconds;

            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                SqlParameter sqlParameter = command.Parameters.Add(new SqlParameter(parameter.Key, parameter.Value));

                if (parameter.Value is IEnumerable<SqlDataRecord>)
                {
                    sqlParameter.TypeName = "Openchain.RecordMutation";
                    sqlParameter.SqlDbType = SqlDbType.Structured;
                }
            }
        }

        #endregion
    }
}
