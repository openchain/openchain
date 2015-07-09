using Microsoft.Data.SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenChain.Core.Sqlite
{
    public class SqliteTransactionStore : ILedgerStore, ILedgerQueries
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

        #region AddLedgerRecord

        public async Task<BinaryData> AddLedgerRecord(BinaryData rawLedgerRecord)
        {
            using (SQLiteTransaction context = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                LedgerRecord record = MessageSerializer.DeserializeLedgerRecord(rawLedgerRecord.ToArray());

                byte[] newLedgerHash = await InsertTransaction(context, record, rawLedgerRecord.ToArray());
                context.Commit();

                return new BinaryData(newLedgerHash);
            }
        }

        private async Task<byte[]> InsertTransaction(SQLiteTransaction context, LedgerRecord ledgerRecord, byte[] rawLedgerRecord)
        {
            byte[] rawTransaction = ledgerRecord.Transaction.ToArray();
            byte[] recordHash = MessageSerializer.ComputeHash(rawLedgerRecord);
            byte[] transactionHash = MessageSerializer.ComputeHash(rawTransaction);

            await UpdateAccounts(MessageSerializer.DeserializeTransaction(rawTransaction), transactionHash);

            await ExecuteAsync(@"
                    INSERT INTO Transactions
                    (TransactionHash, RecordHash, RawData)
                    VALUES (@transactionHash, @recordHash, @rawData)",
                new Dictionary<string, object>()
                {
                    { "@transactionHash", transactionHash },
                    { "@recordHash", recordHash },
                    { "@rawData", rawLedgerRecord }
                });

            return recordHash;
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
                    FROM    Transactions
                    ORDER BY Id DESC
                    LIMIT 1",
                reader => new BinaryData((byte[])reader.GetValue(0)),
                new Dictionary<string, object>());

            return accounts.FirstOrDefault() ?? BinaryData.Empty;
        }

        #endregion

        public async Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetAccounts(IEnumerable<AccountKey> accountKeys)
        {
            Dictionary<AccountKey, AccountEntry> result = new Dictionary<AccountKey, AccountEntry>();

            foreach (AccountKey accountKey in accountKeys)
            {
                SQLiteCommand selectAccount = connection.CreateCommand();
                selectAccount.CommandText = @"
                    SELECT  Balance, Version
                    FROM    Accounts
                    WHERE   Account = @account AND Asset = @asset";

                selectAccount.Parameters.AddWithValue("@account", accountKey.Account);
                selectAccount.Parameters.AddWithValue("@asset", accountKey.Asset);

                using (DbDataReader reader = await selectAccount.ExecuteReaderAsync())
                {
                    bool exists = await reader.ReadAsync();

                    if (exists)
                        result[accountKey] = new AccountEntry(accountKey, reader.GetInt64(0), new BinaryData((byte[])reader.GetValue(1)));
                    else
                        result[accountKey] = new AccountEntry(accountKey, 0, BinaryData.Empty);
                }
            }

            return new ReadOnlyDictionary<AccountKey, AccountEntry>(result);
        }

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
                        FROM    Transactions
                        WHERE   Id > (SELECT Id FROM Transactions WHERE RecordHash = @recordHash)",
                    selector,
                    new Dictionary<string, object>()
                    {
                        { "@recordHash", from.ToArray() }
                    });
            }
            else
            {
                return await ExecuteAsync(@"
                        SELECT  RawData
                        FROM    Transactions",
                    selector,
                    new Dictionary<string, object>());
            }
        }

        #endregion

        public async Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetSubaccounts(string rootAccount)
        {
             IEnumerable<AccountEntry> accounts = await ExecuteAsync(@"
                    SELECT  Account, Asset, Balance, Version
                    FROM    Accounts
                    WHERE   Account GLOB @prefix",
                reader => new AccountEntry(new AccountKey(reader.GetString(0), reader.GetString(1)), reader.GetInt64(2), new BinaryData((byte[])reader.GetValue(3))),
                new Dictionary<string, object>()
                {
                    { "@prefix", rootAccount.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]") + "*" }
                });

            return new ReadOnlyDictionary<AccountKey, AccountEntry>(accounts.ToDictionary(item => item.AccountKey, item => item));
        }

        public async Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetAccount(string account)
        {
            IEnumerable<AccountEntry> accounts = await ExecuteAsync(@"
                    SELECT  Account, Asset, Balance, Version
                    FROM    Accounts
                    WHERE   Account = @account",
               reader => new AccountEntry(new AccountKey(reader.GetString(0), reader.GetString(1)), reader.GetInt64(2), new BinaryData((byte[])reader.GetValue(3))),
               new Dictionary<string, object>()
               {
                    { "@account", account }
               });

            return new ReadOnlyDictionary<AccountKey, AccountEntry>(accounts.ToDictionary(item => item.AccountKey, item => item));
        }

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
