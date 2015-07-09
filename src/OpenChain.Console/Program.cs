using OpenChain.Core;
using OpenChain.Core.Sqlite;
using OpenChain.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Console
{
    public class Program
    {
        static void Main(string[] args)
        {

            Run().Wait();

            
        }

        private static async Task Run()
        {
            //LedgerController.VerifyEvidence();
            
            SqliteTransactionStore store = new SqliteTransactionStore(@"D:\Flavien\Documents\Visual Studio 2015\Projects\OpenChain\Server\src\OpenChain.Console\ledger.db");
            await store.OpenDatabase();

            // DB
            var key = new AccountKey("a/b/cd", "d/e/f");
            var accounts = await store.GetAccounts(new[] { key });

            BinaryData version = BinaryData.Parse("");
            Transaction transaction = new Transaction(
                new[]
                {
                    new AccountEntry(key, 250, accounts[key].Version),
                    new AccountEntry(new AccountKey("a/b/c", "d/e/g"), -250, version)
                },
                version
            );

            byte[] b = MessageSerializer.SerializeTransaction(transaction);

            Transaction res = MessageSerializer.DeserializeTransaction(b);

            //LedgerRecord rec = new LedgerRecord(new BinaryData(b), DateTime.UtcNow, BinaryData.Parse("123456"), 1);

            //await store.AddLedgerRecord(rec);
            await store.AddLedgerRecord(new BinaryData(MessageSerializer.SerializeLedgerRecord(new LedgerRecord(new BinaryData(b), DateTime.UtcNow, BinaryData.Parse("123456")))));
        }
    }
}
