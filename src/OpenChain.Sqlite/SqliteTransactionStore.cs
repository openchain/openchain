using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SQLite;

namespace OpenChain.Sqlite
{
    public class SqliteTransactionStore : ITransactionStore
    {
        public SqliteTransactionStore(string filename)
        {
            this.Connection = new SQLiteConnection(new SQLiteConnectionStringBuilder() { Filename = filename }.ToString());
            this.Connection.OpenAsync().Wait();
        }

        protected SQLiteConnection Connection { get; }

        #region OpenDatabase

        public virtual async Task EnsureTables()
        {
            SQLiteCommand command = Connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions
                (
                    Id INTEGER PRIMARY KEY,
                    Hash BLOB UNIQUE,
                    MutationHash BLOB UNIQUE,
                    RawData BLOB
                );

                CREATE TABLE IF NOT EXISTS Records
                (
                    Key BLOB PRIMARY KEY,
                    Value BLOB,
                    Version BLOB
                );

                CREATE TABLE IF NOT EXISTS Global
                (
                    Id INTEGER PRIMARY KEY,
                    LastTransactionId INT
                );
            ";

            await command.ExecuteNonQueryAsync();

            await ExecuteAsync(@"
                    INSERT OR IGNORE INTO Global
                    (Id, LastTransactionId)
                    VALUES (0, 0)",
                new Dictionary<string, object>());
        }

        #endregion

        #region AddLedgerRecords

        public async Task AddTransactions(IEnumerable<BinaryData> transactions)
        {
            using (SQLiteTransaction context = Connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                long transactionId = (await ExecuteAsync(@"
                        SELECT  LastTransactionId
                        FROM    Global
                        WHERE   Id = 0",
                    reader => reader.GetInt64(0),
                    new Dictionary<string, object>()))
                    .First();

                foreach (BinaryData rawTransaction in transactions)
                {
                    byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                    Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);

                    byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                    Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                    await UpdateAccounts(mutation, mutationHash);

                    transactionId += 1;
                    await ExecuteAsync(@"
                            INSERT INTO Transactions
                            (Id, Hash, MutationHash, RawData)
                            VALUES (@id, @hash, @mutationHash, @rawData)",
                        new Dictionary<string, object>()
                        {
                            ["@id"] = transactionId,
                            ["@hash"] = transactionHash,
                            ["@mutationHash"] = mutationHash,
                            ["@rawData"] = rawTransactionBuffer
                        });

                    await AddTransaction(mutation, mutationHash);
                }

                await ExecuteAsync(@"
                        UPDATE  Global
                        SET     LastTransactionId = @lastTransactionId
                        WHERE   Id = 0",
                    new Dictionary<string, object>()
                    {
                        ["@lastTransactionId"] = transactionId
                    });

                context.Commit();
            }
        }

        protected virtual Task AddTransaction(Mutation mutation, byte[] mutationHash)
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
                        if (!record.Version.Equals(BinaryData.Empty))
                            throw new ConcurrentMutationException(record);
                    }
                    else
                    {
                        if (!new BinaryData(versions[0]).Equals(record.Version))
                            throw new ConcurrentMutationException(record);
                    }
                }
                else
                {
                    if (!record.Version.Equals(BinaryData.Empty))
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
                        catch (SQLiteException exception) when (exception.Message == "constraint failed")
                        {
                            throw new ConcurrentMutationException(record);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetValues

        public async Task<IList<Record>> GetRecords(IEnumerable<BinaryData> keys)
        {
            Dictionary<BinaryData, Record> result = new Dictionary<BinaryData, Record>();

            foreach (BinaryData key in keys)
            {
                SQLiteCommand query = Connection.CreateCommand();
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
                            reader.GetValue(0) == null ? BinaryData.Empty : new BinaryData((byte[])reader.GetValue(0)),
                            new BinaryData((byte[])reader.GetValue(1)));
                    }
                    else
                    {
                        result[key] = new Record(key, BinaryData.Empty, BinaryData.Empty);
                    }
                }
            }

            return result.Values.ToList().AsReadOnly();
        }

        #endregion

        #region GetLastRecord

        public async Task<BinaryData> GetLastTransaction()
        {
            IEnumerable<BinaryData> accounts = await ExecuteAsync(@"
                    SELECT  Hash
                    FROM    Transactions
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
                        ["@recordHash"] = from.ToByteArray()
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
