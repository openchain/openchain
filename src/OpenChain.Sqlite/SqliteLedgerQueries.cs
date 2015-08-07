using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SQLite;
using OpenChain.Ledger;

namespace OpenChain.Sqlite
{
    public class SqliteLedgerQueries : SqliteTransactionStore, ILedgerQueries
    {
        public SqliteLedgerQueries(string filename)
            : base(filename)
        {
        }

        public override async Task EnsureTables()
        {
            await base.EnsureTables();

            SQLiteCommand command = Connection.CreateCommand();
            command.CommandText = @"
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

        protected override async Task AddTransaction(Mutation mutation, byte[] mutationHash)
        {
            foreach (Record record in mutation.Records)
            {
                RecordKey key = RecordKey.Parse(record.Key);
                if (key.RecordType == RecordType.Account)
                {
                    AccountStatus account = AccountStatus.FromRecord(key, record);

                    if (!account.Version.Equals(BinaryData.Empty))
                    {
                        await ExecuteAsync(@"
                                UPDATE  Accounts
                                SET     Balance = @balance, Version = @version
                                WHERE   Account = @account AND Asset = @asset AND Version = @previousVersion",
                            new Dictionary<string, object>()
                            {
                                ["@account"] = account.AccountKey.Account.FullPath,
                                ["@asset"] = account.AccountKey.Asset.FullPath,
                                ["@previousVersion"] = account.Version.Value.ToArray(),
                                ["@balance"] = account.Balance,
                                ["@version"] = mutationHash
                            });
                    }
                    else
                    {
                        await ExecuteAsync(@"
                                INSERT INTO Accounts
                                (Account, Asset, Balance, Version)
                                VALUES (@account, @asset, @balance, @version)",
                            new Dictionary<string, object>()
                            {
                                ["@account"] = account.AccountKey.Account.FullPath,
                                ["@asset"] = account.AccountKey.Asset.FullPath,
                                ["@balance"] = account.Balance,
                                ["@version"] = mutationHash
                            });
                    }
                }
            }
        }

        public async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetSubaccounts(string rootAccount)
        {
            IEnumerable<AccountStatus> accounts = await ExecuteAsync(@"
                    SELECT  Account, Asset, Balance, Version
                    FROM    Accounts
                    WHERE   Account GLOB @prefix",
               reader => new AccountStatus(AccountKey.Parse(reader.GetString(0), reader.GetString(1)), reader.GetInt64(2), new BinaryData((byte[])reader.GetValue(3))),
               new Dictionary<string, object>()
               {
                   ["@prefix"] = rootAccount.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]") + "*"
               });

            return new ReadOnlyDictionary<AccountKey, AccountStatus>(accounts.ToDictionary(item => item.AccountKey, item => item));
        }

        public async Task<IReadOnlyList<AccountStatus>> GetAccount(string account)
        {
            BinaryData prefix = new BinaryData(Encoding.UTF8.GetBytes(account + ":ACC:"));
            IReadOnlyList<Record> records = await GetKeyStartingFrom(prefix);

            return records
                .Select(record => AccountStatus.FromRecord(RecordKey.Parse(record.Key), record))
                .ToList()
                .AsReadOnly();
        }

        public async Task<BinaryData> GetTransaction(BinaryData mutationHash)
        {
            IEnumerable<BinaryData> transactions = await ExecuteAsync(@"
                    SELECT  RawData
                    FROM    Transactions
                    WHERE   MutationHash = @mutationHash",
               reader => new BinaryData((byte[])reader.GetValue(0)),
               new Dictionary<string, object>()
               {
                   ["@mutationHash"] = mutationHash.ToByteArray()
               });

            return transactions.FirstOrDefault();
        }

        public async Task<IReadOnlyList<Record>> GetKeyStartingFrom(BinaryData prefix)
        {
            byte[] from = prefix.ToByteArray();
            byte[] to = prefix.ToByteArray();

            if (to[to.Length - 1] < 255)
                to[to.Length - 1] += 1;

            return await ExecuteAsync(@"
                    SELECT  Key, Value, Version
                    FROM    Records
                    WHERE   Key >= @from AND Key < @to",
            reader => new Record(
                    new BinaryData((byte[])reader.GetValue(0)),
                    reader.GetValue(1) == null ? BinaryData.Empty : new BinaryData((byte[])reader.GetValue(1)),
                    new BinaryData((byte[])reader.GetValue(2))),
                new Dictionary<string, object>()
                {
                    ["@from"] = from,
                    ["@to"] = to
                });
        }
    }
}
