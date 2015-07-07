using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using OpenChain.Core;
using OpenChain.Core.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("query")]
    public class QueryController : Controller
    {
        private readonly ILedgerQueries store;

        public QueryController()
        {
            this.store = new SqliteTransactionStore(@"D:\Flavien\Documents\Visual Studio 2015\Projects\OpenChain\src\OpenChain.Console\ledger.db");
        }

        [HttpGet("accountentry")]
        public async Task<ActionResult> GetAccount(string account, string asset)
        {
            IReadOnlyDictionary<AccountKey, AccountEntry> accounts = await this.store.GetAccounts(new[] { new AccountKey(account, asset) });

            return Json(GetAccountJson(accounts.First().Value));
        }

        [HttpGet("subaccounts")]
        public async Task<ActionResult> GetSubaccounts(string account)
        {
            IReadOnlyDictionary<AccountKey, AccountEntry> accounts = await this.store.GetSubaccounts(account);

            return Json(accounts.Values.Select(GetAccountJson).ToArray());
        }

        private object GetAccountJson(AccountEntry accountEntry)
        {
            return new
            {
                account = accountEntry.AccountKey.Account,
                asset = accountEntry.AccountKey.Asset,
                amount = accountEntry.Amount,
                version = accountEntry.Version.ToString()
            };
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            if (context.Exception is NotSupportedException)
            {
                context.Result = new HttpStatusCodeResult(501);
                context.ExceptionHandled = true;
            }
        }
    }
}
