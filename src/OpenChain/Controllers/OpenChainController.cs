using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.ConfigurationModel;
using Newtonsoft.Json.Linq;
using OpenChain.Core;
using OpenChain.Server;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("")]
    public class OpenChainController : Controller
    {
        private readonly ILedgerStore store;
        private readonly TransactionValidator validator;

        public OpenChainController(IConfiguration configuration, ILedgerStore store, IRulesValidator validator)
        {
            this.store = store;
            this.validator = new TransactionValidator(store, validator);
        }

        [HttpGet("stream")]
        public async Task<ActionResult> GetStream(string from)
        {
            IConfigurationSourceRoot configuration = (IConfigurationSourceRoot)this.Context.ApplicationServices.GetService(typeof(IConfigurationSourceRoot));

            BinaryData ledgerRecordHash;
            if (string.IsNullOrEmpty(from))
                ledgerRecordHash = null;
            else
                ledgerRecordHash = BinaryData.Parse(from);

            IReadOnlyList<BinaryData> records = await this.store.GetTransactionStream(ledgerRecordHash);

            return Json(records.Select(record => new { raw = record.ToString() }).ToArray());
        }

        [HttpPost("submit")]
        public async Task<ActionResult> Post([FromBody]JObject body)
        {
            BinaryData parsedTransaction = BinaryData.Parse((string)body["raw"]);

            BinaryData ledgerRecordHash = await validator.PostTransaction(parsedTransaction, new AuthenticationEvidence[0]);

            return Json(new
            {
                ledger_record = ledgerRecordHash.ToString()
            });
        }
    }
}
