using Microsoft.Data.SQLite;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Core.Sqlite
{
    public partial class SqliteLedgerStore : ILedgerQueries
    {
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
    }
}
