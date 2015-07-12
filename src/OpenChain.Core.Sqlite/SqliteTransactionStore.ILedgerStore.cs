using Microsoft.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Core.Sqlite
{
    public partial class SqliteTransactionStore : ITransactionStore
    {
        private readonly SQLiteConnection connection;

        public SqliteTransactionStore(string filename)
        {
            this.connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() { Filename = filename }.ToString());
            this.OpenDatabase().Wait();
        }

        #region OpenDatabase

        public async Task OpenDatabase()
        {
            await connection.OpenAsync();

            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Hash BLOB UNIQUE,
                    MutationSetHash BLOB UNIQUE,
                    RawData BLOB
                );

                CREATE TABLE IF NOT EXISTS KeyValuePairs
                (
                    Key BLOB PRIMARY KEY,
                    Value BLOB,
                    Version BLOB
                );
            ";

            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region AddLedgerRecords

        public async Task AddTransactions(IEnumerable<BinaryData> rawTransactions)
        {
            using (SQLiteTransaction context = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                foreach (BinaryData rawTransaction in rawTransactions)
                {
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransaction);

                    byte[] mutationSetHash = MessageSerializer.ComputeHash(transaction.MutationSet);

                    await UpdateAccounts(MessageSerializer.DeserializeMutationSet(rawTransaction), mutationSetHash);
                    
                    await ExecuteAsync(@"
                            INSERT INTO Transactions
                            (Hash, MutationSetHash, RawData)
                            VALUES (@hash, @mutationSetHash, @rawData)",
                        new Dictionary<string, object>()
                        {
                            { "@hash", transactionHash },
                            { "@mutationSetHash", mutationSetHash },
                            { "@rawData", rawTransaction.ToByteArray() }
                        });
                }

                context.Commit();
            }
        }

        private async Task UpdateAccounts(MutationSet mutationSet, byte[] transactionHash)
        {
            foreach (Mutation mutation in mutationSet.Mutations)
            {
                if (!mutation.Version.Equals(BinaryData.Empty))
                {
                    // Update existing account
                    int count = await ExecuteAsync(@"
                            UPDATE  KeyValuePairs
                            SET     Value = @value, Version = @version
                            WHERE   Key = @key",
                        new Dictionary<string, object>()
                        {
                            { "@key", mutation.Key.ToByteArray() },
                            { "@value", mutation.Value.ToByteArray() },
                            { "@version", mutation.Version.ToByteArray() }
                        });

                    if (count == 0)
                        throw new ConcurrentMutationException(mutation);
                }
                else
                {
                    // Create new account
                    try
                    {
                        await ExecuteAsync(@"
                                INSERT INTO KeyValuePairs
                                (Key, Value, Version)
                                VALUES (@key, @value, @version)",
                            new Dictionary<string, object>()
                            {
                                { "@key", mutation.Key.ToByteArray() },
                                { "@value", mutation.Value.ToByteArray() },
                                { "@version", transactionHash }
                            });
                    }
                    catch (SQLiteException exception) when (exception.Message == "constraint failed")
                    {
                        throw new ConcurrentMutationException(mutation);
                    }
                }
            }
        }

        #endregion

        #region GetLastRecord

        public async Task<BinaryData> GetLastTransaction()
        {
            IEnumerable<BinaryData> accounts = await ExecuteAsync(@"
                    SELECT  RecordHash
                    FROM    Records
                    ORDER BY Id DESC
                    LIMIT 1",
                reader => new BinaryData((byte[])reader.GetValue(0)),
                new Dictionary<string, object>());

            return accounts.FirstOrDefault() ?? BinaryData.Empty;
        }

        #endregion
        
        #region GetRecordStream

        public IObservable<BinaryData> GetTransactionStream(BinaryData from)
        {
            return new PollingObservable(from, this.GetLedgerRecords);
        }

        private async Task<IReadOnlyList<BinaryData>> GetLedgerRecords(BinaryData from)
        {
            Func<DbDataReader, BinaryData> selector = reader => new BinaryData((byte[])reader.GetValue(0));
            if (from != null)
            {
                return await ExecuteAsync(@"
                        SELECT  RawData
                        FROM    Records
                        WHERE   Id > (SELECT Id FROM Records WHERE RecordHash = @recordHash)
                        ORDER BY Id ASC",
                    selector,
                    new Dictionary<string, object>()
                    {
                        { "@recordHash", from.ToByteArray() }
                    });
            }
            else
            {
                return await ExecuteAsync(@"
                        SELECT  RawData
                        FROM    Records
                        ORDER BY Id ASC",
                    selector,
                    new Dictionary<string, object>());
            }
        }

        #endregion
        
        #region Private Methods

        private async Task<IReadOnlyList<T>> ExecuteAsync<T>(string commandText, Func<DbDataReader, T> selector, IDictionary<string, object> parameters)
        {
            SQLiteCommand query = connection.CreateCommand();
            query.CommandText = commandText;

            foreach (KeyValuePair<string, object> parameter in parameters)
                query.Parameters.AddWithValue(parameter.Key, parameter.Value);

            List<T> result = new List<T>();
            using (DbDataReader reader = await query.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    result.Add(selector(reader));
            }

            return result.AsReadOnly();
        }

        private async Task<int> ExecuteAsync(string commandText, IDictionary<string, object> parameters)
        {
            SQLiteCommand query = connection.CreateCommand();
            query.CommandText = commandText;

            foreach (KeyValuePair<string, object> parameter in parameters)
                query.Parameters.AddWithValue(parameter.Key, parameter.Value);

            return await query.ExecuteNonQueryAsync();
        }

        #endregion
    }
}
