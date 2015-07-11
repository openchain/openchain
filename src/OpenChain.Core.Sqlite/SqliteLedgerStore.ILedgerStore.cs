using Microsoft.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Core.Sqlite
{
    public partial class SqliteLedgerStore : ILedgerStore
    {
        private readonly SQLiteConnection connection;

        public SqliteLedgerStore(string filename)
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
                CREATE TABLE IF NOT EXISTS Records
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TransactionHash BLOB UNIQUE,
                    RecordHash BLOB UNIQUE,
                    RawData BLOB
                );

                CREATE TABLE IF NOT EXISTS Accounts
                (
                    Account TEXT,
                    Asset TEXT,
                    Balance INTEGER,
                    Version BLOB,
                    PRIMARY KEY (Account ASC, Asset ASC)
                );
            ";

            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region AddLedgerRecords

        public async Task AddLedgerRecords(IEnumerable<BinaryData> rawLedgerRecords)
        {
            using (SQLiteTransaction context = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                foreach (BinaryData rawLedgerRecord in rawLedgerRecords)
                {
                    byte[] ledgerRecordData = rawLedgerRecord.ToByteArray();
                    LedgerRecord record = MessageSerializer.DeserializeLedgerRecord(ledgerRecordData);

                    byte[] rawTransaction = record.Transaction.ToByteArray();
                    byte[] transactionHash = MessageSerializer.ComputeHash(rawTransaction);

                    await UpdateAccounts(MessageSerializer.DeserializeTransaction(rawTransaction), transactionHash);

                    byte[] recordHash = MessageSerializer.ComputeHash(ledgerRecordData);

                    await ExecuteAsync(@"
                            INSERT INTO Records
                            (TransactionHash, RecordHash, RawData)
                            VALUES (@transactionHash, @recordHash, @rawData)",
                        new Dictionary<string, object>()
                        {
                            { "@transactionHash", transactionHash },
                            { "@recordHash", recordHash },
                            { "@rawData", ledgerRecordData }
                        });
                }

                context.Commit();
            }
        }

        private async Task UpdateAccounts(Transaction transaction, byte[] transactionHash)
        {
            foreach (AccountEntry entry in transaction.AccountEntries)
            {
                if (!entry.Version.Equals(BinaryData.Empty))
                {
                    // Update existing account
                    int count = await ExecuteAsync(@"
                            UPDATE  Accounts
                            SET     Balance = Balance + @balance, Version = @version
                            WHERE   Account = @account AND Asset = @asset AND Version = @previousVersion",
                        new Dictionary<string, object>()
                        {
                            { "@account", entry.AccountKey.Account },
                            { "@asset", entry.AccountKey.Asset },
                            { "@previousVersion", entry.Version.Value.ToArray() },
                            { "@balance", entry.Amount },
                            { "@version", transactionHash }
                        });

                    if (count == 0)
                        throw new AccountModifiedException(entry);
                }
                else
                {
                    // Create new account
                    try
                    {
                        await ExecuteAsync(@"
                                INSERT INTO Accounts
                                (Account, Asset, Balance, Version)
                                VALUES (@account, @asset, @balance, @version)",
                            new Dictionary<string, object>()
                            {
                                { "@account", entry.AccountKey.Account },
                                { "@asset", entry.AccountKey.Asset },
                                { "@balance", entry.Amount },
                                { "@version", transactionHash }
                            });
                    }
                    catch (SQLiteException exception) when (exception.Message == "constraint failed")
                    {
                        throw new AccountModifiedException(entry);
                    }
                }
            }
        }

        #endregion

        #region GetLastRecord

        public async Task<BinaryData> GetLastRecord()
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

        public IObservable<BinaryData> GetRecordStream(BinaryData from)
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
