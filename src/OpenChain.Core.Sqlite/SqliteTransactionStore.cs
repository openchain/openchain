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

        public async Task<BinaryData> AddTransaction(BinaryData rawTransaction, DateTime timestamp, BinaryData externalMetadata)
        {
            using (SQLiteTransaction context = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                Tuple<BinaryData, long> ledgerStatus = await GetLedgerStatus(context);

                LedgerRecord ledgerRecord = new LedgerRecord(
                    rawTransaction,
                    timestamp,
                    externalMetadata,
                    ledgerStatus.Item1);

                byte[] newLedgerHash = await InsertTransaction(context, ledgerRecord, TransactionSerializer.SerializeLedgerRecord(ledgerRecord), ledgerStatus.Item2 + 1);

                context.Commit();

                return new BinaryData(newLedgerHash);
            }
        }

        public async Task<BinaryData> AddLedgerRecord(BinaryData rawLedgerRecord)
        {
            using (SQLiteTransaction context = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                Tuple<BinaryData, long> ledgerStatus = await GetLedgerStatus(context);

                LedgerRecord record = TransactionSerializer.DeserializeLedgerRecord(rawLedgerRecord.ToArray());

                if (!record.PreviousRecordHash.Equals(ledgerStatus.Item1))
                    throw new InvalidOperationException();

                byte[] newLedgerHash = await InsertTransaction(context, record, rawLedgerRecord.ToArray(), ledgerStatus.Item2 + 1);
                context.Commit();

                return new BinaryData(newLedgerHash);
            }
        }

        private async Task<Tuple<BinaryData, long>> GetLedgerStatus(SQLiteTransaction context)
        {
            SQLiteCommand ledgerVersion = connection.CreateCommand();
            ledgerVersion.CommandText = @"
                SELECT  LedgerHeight, LedgerHash
                FROM    Ledgers
                WHERE   Id = 0";

            BinaryData lastTransactionHash;
            long ledgerHeight;
            using (DbDataReader reader = await ledgerVersion.ExecuteReaderAsync())
            {
                bool ledgerFound = await reader.ReadAsync();
                if (!ledgerFound)
                    throw new InvalidOperationException();

                ledgerHeight = reader.GetInt64(0);
                lastTransactionHash = new BinaryData((byte[])reader.GetValue(1) ?? new byte[0]);
            }

            return Tuple.Create(lastTransactionHash, ledgerHeight);
        }

        private async Task<byte[]> InsertTransaction(SQLiteTransaction context, LedgerRecord ledgerRecord, byte[] rawLedgerRecord, long id)
        {
            byte[] rawTransaction = ledgerRecord.Payload.ToArray();
            byte[] recordHash;
            byte[] transactionHash;
            using (SHA256 hash = SHA256.Create())
            {
                recordHash = hash.ComputeHash(rawLedgerRecord);
                transactionHash = hash.ComputeHash(rawTransaction);
            }

            await UpdateAccounts(TransactionSerializer.DeserializeTransaction(rawTransaction), transactionHash);

            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Transactions
                (Id, TransactionHash, RecordHash, PreviousRecordHash, RawData)
                VALUES (@id, @transactionHash, @recordHash, @previousRecordHash, @rawData)";

            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@transactionHash", transactionHash);
            command.Parameters.AddWithValue("@recordHash", recordHash);
            command.Parameters.AddWithValue("@previousRecordHash", ledgerRecord.PreviousRecordHash.ToArray());
            command.Parameters.AddWithValue("@rawData", rawLedgerRecord);

            await command.ExecuteNonQueryAsync();

            SQLiteCommand updateLedgerHash = connection.CreateCommand();
            updateLedgerHash.CommandText = @"
                UPDATE  Ledgers
                SET     LedgerHash = @ledgerHash,
                        LedgerHeight = LedgerHeight + 1
                WHERE   Id = 0";
            updateLedgerHash.Parameters.AddWithValue("@ledgerHash", recordHash);
            await updateLedgerHash.ExecuteNonQueryAsync();
            
            return recordHash;
        }

        private async Task UpdateAccounts(Transaction transaction, byte[] transactionHash)
        {
            var accountEntries = transaction.AccountEntries
                .GroupBy(entry => entry.AccountKey, entry => entry);

            if (accountEntries.Any(group => group.Count() > 1))
                throw new InvalidOperationException();

            foreach (AccountEntry entry in transaction.AccountEntries)
            {
                if (!entry.Version.Equals(BinaryData.Empty))
                {
                    SQLiteCommand insertAccount = connection.CreateCommand();
                    insertAccount.CommandText = @"
                            UPDATE  Accounts
                            SET     Balance = Balance + @balance, Version = @version
                            WHERE   Account = @account AND Asset = @asset AND Version = @previousVersion";
                    insertAccount.Parameters.AddWithValue("@account", entry.AccountKey.Account);
                    insertAccount.Parameters.AddWithValue("@asset", entry.AccountKey.Asset);
                    insertAccount.Parameters.AddWithValue("@previousVersion", entry.Version.Value.ToArray());
                    insertAccount.Parameters.AddWithValue("@balance", entry.Amount);
                    insertAccount.Parameters.AddWithValue("@version", transactionHash);

                    int count = await insertAccount.ExecuteNonQueryAsync();

                    if (count == 0)
                        throw new AccountModifiedException(entry);
                }
                else
                {
                    SQLiteCommand insertAccount = connection.CreateCommand();
                    insertAccount.CommandText = @"
                            INSERT INTO Accounts
                            (Account, Asset, Balance, Version)
                            VALUES (@account, @asset, @balance, @version)";
                    insertAccount.Parameters.AddWithValue("@account", entry.AccountKey.Account);
                    insertAccount.Parameters.AddWithValue("@asset", entry.AccountKey.Asset);
                    insertAccount.Parameters.AddWithValue("@balance", entry.Amount);
                    insertAccount.Parameters.AddWithValue("@version", transactionHash);

                    try
                    {
                        await insertAccount.ExecuteNonQueryAsync();
                    }
                    catch (SQLiteException exception) when (exception.Message == "constraint failed")
                    {
                        throw new AccountModifiedException(entry);
                    }
                }
            }
        }

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

        public async Task<IReadOnlyList<BinaryData>> GetTransactionStream(BinaryData from)
        {
            SQLiteCommand query = connection.CreateCommand();
            if (from != null)
            {
                query.CommandText = @"
                    SELECT  RawData
                    FROM    Transactions
                    WHERE   Id > (SELECT Id FROM Transactions WHERE RecordHash = @recordHash)";
                query.Parameters.AddWithValue("@recordHash", from.ToArray());
            }
            else
            {
                query.CommandText = @"
                    SELECT  RawData
                    FROM    Transactions";
            }

            List<BinaryData> result = new List<BinaryData>();
            using (DbDataReader reader = await query.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    result.Add(new BinaryData((byte[])reader.GetValue(0)));
            }

            return result.AsReadOnly();
        }

        public async Task OpenDatabase()
        {
            await connection.OpenAsync();

            SQLiteCommand command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Transactions
                (
                    Id INTEGER PRIMARY KEY,
                    TransactionHash BLOB UNIQUE,
                    RecordHash BLOB UNIQUE,
                    PreviousRecordHash BLOB,
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

                CREATE TABLE IF NOT EXISTS Ledgers
                (
                    Id INT,
                    LedgerHeight INT,
                    LedgerHash BLOB,
                    PRIMARY KEY (Id ASC)
                );
            ";

            await command.ExecuteNonQueryAsync();

            // Insert the ledger master record
            command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO Ledgers
                (Id, LedgerHeight, LedgerHash)
                VALUES (0, 0, X'');
            ";

            await command.ExecuteNonQueryAsync();
        }

        public Task<IReadOnlyDictionary<AccountKey, AccountEntry>> GetSubaccounts(string rootAccount)
        {
            throw new NotSupportedException();
        }
    }
}
