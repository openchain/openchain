using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json.Linq;
using OpenChain.Core;
using OpenChain.Server;
using OpenChain.Core.Sqlite;
using Microsoft.AspNet.Http;
using OpenChain.Models;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Cors.Core;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace OpenChain.Controllers
{
    [Route("")]
    public class OpenChainController : Controller
    {
        private readonly ILedgerStore store;
        
        public OpenChainController(IConfiguration configuration, ILedgerStore store)
        {
            this.store = store;
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

            // Validate deserialization
            Transaction deserializedTransaction = TransactionSerializer.DeserializeTransaction(parsedTransaction.ToArray());

            BinaryData ledgerRecord = await this.store.AddTransaction(parsedTransaction, DateTime.UtcNow, BinaryData.Empty);

            return Json(new
            {
                ledger_record = ledgerRecord.ToString()
            });
        }
    }
}
