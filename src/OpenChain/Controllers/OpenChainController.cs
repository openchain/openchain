using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.ConfigurationModel;
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

        public OpenChainController(IConfiguration configuration, ILedgerStore store, IRulesValidator validator)
        {
            this.configuration = configuration;
            this.store = store;
            this.validator = new TransactionValidator(store, validator);
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
            catch (AccountModifiedException)
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.PreconditionFailed);
            }

            return Json(new
            {
                ledger_record = ledgerRecordHash.ToString()
            });
        }
    }
}
