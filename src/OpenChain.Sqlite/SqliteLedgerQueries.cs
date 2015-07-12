using Microsoft.Data.SQLite;
using OpenChain.Core;
using OpenChain.Ledger;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Sqlite
{
    public class SqliteLedgerQueries : SqliteTransactionStore, ILedgerQueries
    {
        public SqliteLedgerQueries(string filename)
            : base(filename)
        {
        }

        public override async Task OpenDatabase()
        {
            await base.OpenDatabase();

            SQLiteCommand command = Connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Accounts
                (
                    Account TEXT UNIQUE,
                    Asset TEXT UNIQUE,
                    Balance INTEGER,
                    Version BLOB,
                    PRIMARY KEY (Account ASC, Asset ASC)
                );
            ";

            await command.ExecuteNonQueryAsync();
        }

        protected override async Task AddTransaction(MutationSet mutationSet, byte[] mutationSetHash)
        {
            foreach (Mutation mutation in mutationSet.Mutations)
            {
                AccountStatus account = AccountStatus.FromMutation(mutation);
                if (account != null)
                {
                    if (!account.Version.Equals(BinaryData.Empty))
                    {
                        await ExecuteAsync(@"
                                UPDATE  Accounts
                                SET     Balance = @balance, Version = @version
                                WHERE   Account = @account AND Asset = @asset AND Version = @previousVersion",
                            new Dictionary<string, object>()
                            {
                                { "@account", account.AccountKey.Account },
                                { "@asset", account.AccountKey.Asset },
                                { "@previousVersion", account.Version.Value.ToArray() },
                                { "@balance", account.Balance },
                                { "@version", mutationSetHash }
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
                                { "@account", account.AccountKey.Account },
                                { "@asset", account.AccountKey.Asset },
                                { "@balance", account.Balance },
                                { "@version", mutationSetHash }
                            });
                    }
                }
            }
        }

        public async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccounts(IEnumerable<AccountKey> accountKeys)
        {
            Dictionary<AccountKey, AccountStatus> result = new Dictionary<AccountKey, AccountStatus>();

            foreach (AccountKey accountKey in accountKeys)
            {
                SQLiteCommand selectAccount = Connection.CreateCommand();
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
                        result[accountKey] = new AccountStatus(accountKey, reader.GetInt64(0), new BinaryData((byte[])reader.GetValue(1)));
                    else
                        result[accountKey] = new AccountStatus(accountKey, 0, BinaryData.Empty);
                }
            }

            return new ReadOnlyDictionary<AccountKey, AccountStatus>(result);
        }

        public async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetSubaccounts(string rootAccount)
        {
             IEnumerable<AccountStatus> accounts = await ExecuteAsync(@"
                    SELECT  Account, Asset, Balance, Version
                    FROM    Accounts
                    WHERE   Account GLOB @prefix",
                reader => new AccountStatus(new AccountKey(reader.GetString(0), reader.GetString(1)), reader.GetInt64(2), new BinaryData((byte[])reader.GetValue(3))),
                new Dictionary<string, object>()
                {
                    { "@prefix", rootAccount.Replace("[", "[[]").Replace("*", "[*]").Replace("?", "[?]") + "*" }
                });

            return new ReadOnlyDictionary<AccountKey, AccountStatus>(accounts.ToDictionary(item => item.AccountKey, item => item));
        }

        public async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> GetAccount(string account)
        {
            IEnumerable<AccountStatus> accounts = await ExecuteAsync(@"
                    SELECT  Account, Asset, Balance, Version
                    FROM    Accounts
                    WHERE   Account = @account",
               reader => new AccountStatus(new AccountKey(reader.GetString(0), reader.GetString(1)), reader.GetInt64(2), new BinaryData((byte[])reader.GetValue(3))),
               new Dictionary<string, object>()
               {
                    { "@account", account }
               });

            return new ReadOnlyDictionary<AccountKey, AccountStatus>(accounts.ToDictionary(item => item.AccountKey, item => item));
        }
    }
}
