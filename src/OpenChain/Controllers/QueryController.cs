using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using OpenChain.Core;
using OpenChain.Core.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using OpenChain.Ledger;

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("query")]
    public class QueryController : Controller
    {
        private readonly ILedgerQueries store;

        public QueryController(ILedgerQueries store)
        {
            this.store = store;
        }

        [HttpGet("accountentries")]
        public async Task<ActionResult> GetAccount(string account, string asset)
        {
            IReadOnlyDictionary<AccountKey, AccountEntry> accounts;
            if (asset != null && account != null)
            {
                accounts = await this.store.GetAccounts(new[] { new AccountKey(account, asset) });
            }
            else if (asset == null && account != null)
            {
                accounts = await this.store.GetAccount(account);
            }
            else
            {
                return HttpBadRequest();
            }

            return Json(accounts.Values.Select(GetAccountJson).ToArray());
        }

        [HttpGet("subaccounts")]
        public async Task<ActionResult> GetSubaccounts(string account)
        {
            LedgerPath path;
            if (!LedgerPath.TryParse(account, out path))
                return HttpBadRequest();

            if (path.IsDirectory)
                return HttpBadRequest();

            LedgerPath directory = LedgerPath.FromSegments(path.Segments.ToArray(), true);

            IReadOnlyDictionary<AccountKey, AccountEntry> accounts = await this.store.GetSubaccounts(directory.FullPath);

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
