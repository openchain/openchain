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
        private readonly TimeSpan commandTimeout;

        public SqlServerStorageEngine(string connectionString, TimeSpan commandTimeout)
        {
            this.Connection = new SqlConnection(connectionString);
            this.commandTimeout = commandTimeout;
        }

        protected SqlConnection Connection { get; }

        #region AddTransactions

        public Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            throw new NotSupportedException();
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

        public async Task<IReadOnlyList<T>> ExecuteQuery<T>(string query, Func<SqlDataReader, T> readRecord, IDictionary<string, object> parameters)
        {
            List<T> result = new List<T>();

            using (SqlCommand command = new SqlCommand(query, this.Connection))
            {
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
                command.Parameters.Add(new SqlParameter(parameter.Key, parameter.Value));
        }

        private void SetTableParameters(string key, string typeName, IEnumerable<SqlDataRecord> records, SqlCommand command)
        {
            SqlParameter sqlParameter = command.Parameters.Add(new SqlParameter(key, records));
            sqlParameter.TypeName = typeName;
        }

        #endregion
    }
}
