using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;
using OpenChain.Core;
using OpenChain.Server;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("")]
    public class OpenChainController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly ILedgerStore store;
        private readonly TransactionValidator validator;
        private readonly ILogger logger;

        public OpenChainController(IConfiguration configuration, ILedgerStore store, IRulesValidator validator, ILogger logger)
        {
            this.configuration = configuration;
            this.store = store;
            this.validator = new TransactionValidator(store, validator, configuration.GetSubKey("Main").Get("root_url"));
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
            if (!configuration.GetSubKey("Main").Get<bool>("is_master"))
                return new HttpStatusCodeResult((int)HttpStatusCode.NotImplemented);

            BinaryData parsedTransaction = BinaryData.Parse((string)body["transaction"]);

            List<AuthenticationEvidence> authentication = new List<AuthenticationEvidence>();

            foreach (JObject evidence in body["authentication"])
            {
                authentication.Add(new AuthenticationEvidence(
                    (string)evidence["identity"],
                    evidence["evidence"].Select(token => BinaryData.Parse((string)token).ToByteArray()).ToArray()));
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

        [HttpGet("info")]
        public ActionResult GetLedgerInformation()
        {
            return Json(new
            {
                root_url = configuration.GetSubKey("Main").Get("root_url"),
                name = configuration.GetSubKey("Info").Get("name")
            });
        }
    }
}
