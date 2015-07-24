using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;
using OpenChain.Core;
using OpenChain.Ledger;
using OpenChain.Models;

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("")]
    public class OpenChainController : Controller
    {
        private readonly ITransactionStore store;
        private readonly ILogger logger;

        public OpenChainController(ITransactionStore store, ILogger logger)
        {
            this.store = store;
            this.logger = logger;
        }

        /// <summary>
        /// Format:
        /// {
        ///   "transaction": "hex",
        ///   "authentication": [
        ///     {
        ///       "identity": "string",
        ///       "evidence": [
        ///         "hex",
        ///         "hex"
        ///       ]
        ///     },
        ///     ...
        ///   ]
        /// }
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost("submit")]
        public async Task<ActionResult> Post([FromBody]JObject body)
        {
            TransactionValidator validator = Context.ApplicationServices.GetService<TransactionValidator>();
            if (validator == null)
                return new HttpStatusCodeResult((int)HttpStatusCode.NotImplemented);

            BinaryData parsedTransaction = BinaryData.Parse((string)body["transaction"]);

            List<SignatureEvidence> authentication = new List<SignatureEvidence>();

            foreach (JObject evidence in body["signatures"])
            {
                authentication.Add(new SignatureEvidence(
                    BinaryData.Parse((string)evidence["pub_key"]),
                    BinaryData.Parse((string)evidence["signature"])));
            }

            BinaryData ledgerRecordHash;
            try
            {
                ledgerRecordHash = await validator.PostTransaction(parsedTransaction, authentication);
            }
            catch (TransactionInvalidException exception)
            {
                logger.LogInformation("Rejected transaction: {0}", exception.Message);
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            }

            logger.LogInformation("Validated transaction {0}", ledgerRecordHash.ToString());

            return Json(new
            {
                ledger_record = ledgerRecordHash.ToString()
            });
        }

        [HttpGet("value")]
        public async Task<ActionResult> GetValue(string key)
        {
            BinaryData parsedKey = BinaryData.Parse(key);

            Record result = (await this.store.GetRecords(new[] { parsedKey })).First();

            return Json(new
            {
                key = parsedKey.ToString(),
                value = result.Value?.ToString(),
                version = result.Version.ToString()
            });
        }

        [HttpGet("info")]
        public ActionResult GetLedgerInformation()
        {
            TransactionValidator validator = Context.ApplicationServices.GetService<TransactionValidator>();
            MasterProperties properties = Context.ApplicationServices.GetService<MasterProperties>();

            if (validator != null)
            {
                if (properties != null)
                    return Json(new
                    {
                        root_url = validator.RootUrl,
                        name = properties.Name,
                        tos = properties.Tos
                    });
                else
                    return Json(new
                    {
                        root_url = validator.RootUrl
                    });
            }
            else
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.NotImplemented);
            }
        }
    }
}
