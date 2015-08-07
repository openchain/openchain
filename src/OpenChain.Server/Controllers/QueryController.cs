using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using OpenChain.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Server.Controllers
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

        [HttpGet("account")]
        public async Task<ActionResult> GetAccount(string account)
        {
            IReadOnlyList<AccountStatus> accounts = await this.store.GetAccount(account);

            return Json(accounts.Select(GetAccountJson).ToArray());
        }

        [HttpGet("transaction")]
        public async Task<ActionResult> GetTransaction(string mutationHash)
        {
            BinaryData transaction = await this.store.GetTransaction(BinaryData.Parse(mutationHash));

            if (transaction == null)
                return new HttpStatusCodeResult(404);
            else
                return Json(new { raw = transaction.ToString() });
        }

        [HttpGet("subaccounts")]
        public async Task<ActionResult> GetSubaccounts(string account)
        {
            LedgerPath path;
            if (!LedgerPath.TryParse(account, out path))
                return HttpBadRequest();

            LedgerPath directory = LedgerPath.FromSegments(path.Segments.ToArray());

            IReadOnlyList<Record> accounts = await this.store.GetSubaccounts(directory.FullPath);

            return Json(accounts.Select(result => new
            {
                key = result.Key.ToString(),
                value = result.Value.ToString(),
                version = result.Version.ToString()
            }).ToArray());
        }

        private object GetAccountJson(AccountStatus account)
        {
            return new
            {
                account = account.AccountKey.Account.FullPath,
                asset = account.AccountKey.Asset.FullPath,
                balance = account.Balance.ToString(),
                version = account.Version.ToString()
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
