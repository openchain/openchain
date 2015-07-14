using Microsoft.Data.SQLite;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Sqlite
{
    public class SqliteTransactionStore : ITransactionStore
    {
        public SqliteTransactionStore(string filename)
        {
            this.Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() { Filename = filename }.ToString());
            this.OpenDatabase().Wait();
        }

        protected SQLiteConnection Connection { get; }

        #region OpenDatabase

        public virtual async Task OpenDatabase()
        {
            await Connection.OpenAsync();

            SQLiteCommand command = Connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Hash BLOB UNIQUE,
                    MutationHash BLOB UNIQUE,
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

        public async Task AddTransactions(IEnumerable<BinaryData> transactions)
        {
            using (SQLiteTransaction context = Connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                foreach (BinaryData rawTransaction in transactions)
                {
                    byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);

                    byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                    await UpdateAccounts(mutation, mutationHash);
                    
                    await ExecuteAsync(@"
                            INSERT INTO Transactions
                            (Hash, MutationHash, RawData)
                            VALUES (@hash, @mutationHash, @rawData)",
                        new Dictionary<string, object>()
                        {
                            { "@hash", transactionHash },
                            { "@mutationHash", mutationHash },
                            { "@rawData", rawTransactionBuffer }
                        });

                    await AddTransaction(mutation, mutationHash);
                }

                context.Commit();
            }
        }

        protected virtual Task AddTransaction(Mutation mutation, byte[] mutationHash)
        {
            return Task.FromResult(0);
        }

        private async Task UpdateAccounts(Mutation mutation, byte[] transactionHash)
        {
            foreach (KeyValuePair pair in mutation.KeyValuePairs)
            {
                if (pair.Value == null)
                {
                    // Read the key-value pair and make sure it corresponds to the one supplied
                    IReadOnlyList<byte[]> versions = await ExecuteAsync<byte[]>(@"
                            SELECT  Version
                            FROM    KeyValuePairs
                            WHERE   Key = @key",
                        reader => (byte[])reader.GetValue(0),
                        new Dictionary<string, object>()
                        {
                            { "@key", pair.Key.ToByteArray() }
                        });

                    if (versions.Count == 0)
                    {
                        if (!pair.Version.Equals(BinaryData.Empty))
                            throw new ConcurrentMutationException(pair);
                    }
                    else
                    {
                        if (!new BinaryData(versions[0]).Equals(pair.Version))
                            throw new ConcurrentMutationException(pair);
                    }
                }
                else
                {
                    if (!pair.Version.Equals(BinaryData.Empty))
                    {
                        // Update existing account
                        int count = await ExecuteAsync(@"
                                UPDATE  KeyValuePairs
                                SET     Version = @version
                                WHERE   Key = @key AND Version = @currentVersion",
                            new Dictionary<string, object>()
                            {
                                { "@key", pair.Key.ToByteArray() },
                                { "@version", transactionHash },
                                { "@currentVersion", pair.Version.ToByteArray() }
                            });

                        if (count == 0)
                            throw new ConcurrentMutationException(pair);
                    }
                    else
                    {
                        // Create a new key-value pair
                        try
                        {
                            await ExecuteAsync(@"
                                    INSERT INTO KeyValuePairs
                                    (Key, Value, Version)
                                    VALUES (@key, @value, @version)",
                                new Dictionary<string, object>()
                                {
                                    { "@key", pair.Key.ToByteArray() },
                                    { "@value", pair.Value.ToByteArray() },
                                    { "@version", transactionHash }
                                });
                        }
                        catch (SQLiteException exception) when (exception.Message == "constraint failed")
                        {
                            throw new ConcurrentMutationException(pair);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetValues

        public async Task<IList<KeyValuePair>> GetValues(IEnumerable<BinaryData> keys)
        {
            Dictionary<BinaryData, KeyValuePair> result = new Dictionary<BinaryData, KeyValuePair>();

            foreach (BinaryData key in keys)
            {
                SQLiteCommand query = Connection.CreateCommand();
                query.CommandText = @"
                    SELECT  Value, Version
                    FROM    KeyValuePairs
                    WHERE   Key = @key";

                query.Parameters.AddWithValue("@key", key.ToByteArray());

                using (DbDataReader reader = await query.ExecuteReaderAsync())
                {
                    bool exists = await reader.ReadAsync();

                    if (exists)
                        result[key] = new KeyValuePair(key, new BinaryData((byte[])reader.GetValue(0)), new BinaryData((byte[])reader.GetValue(1)));
                    else
                        result[key] = new KeyValuePair(key, null, BinaryData.Empty);
                }
            }

            return result.Values.ToList().AsReadOnly();
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

        protected async Task<IReadOnlyList<T>> ExecuteAsync<T>(string commandText, Func<DbDataReader, T> selector, IDictionary<string, object> parameters)
        {
            SQLiteCommand query = Connection.CreateCommand();
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

        protected async Task<int> ExecuteAsync(string commandText, IDictionary<string, object> parameters)
        {
            SQLiteCommand query = Connection.CreateCommand();
            query.CommandText = commandText;

            foreach (KeyValuePair<string, object> parameter in parameters)
                query.Parameters.AddWithValue(parameter.Key, parameter.Value);

            return await query.ExecuteNonQueryAsync();
        }

        #endregion
    }
}
