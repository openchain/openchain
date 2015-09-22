// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;
using OpenChain.Ledger;
using OpenChain.Ledger.Validation;
using OpenChain.Server.Models;

namespace OpenChain.Server.Controllers
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

            ByteString parsedTransaction = ByteString.Parse((string)body["transaction"]);

            List<SignatureEvidence> authentication = new List<SignatureEvidence>();

            foreach (JObject evidence in body["signatures"])
            {
                authentication.Add(new SignatureEvidence(
                    ByteString.Parse((string)evidence["pub_key"]),
                    ByteString.Parse((string)evidence["signature"])));
            }

            ByteString ledgerRecordHash;
            try
            {
                ledgerRecordHash = await validator.PostTransaction(parsedTransaction, authentication);
            }
            catch (TransactionInvalidException exception)
            {
                logger.LogInformation("Rejected transaction: {0}", exception.Message);

                JsonResult result = Json(new
                {
                    error_code = exception.Reason
                });

                result.StatusCode = (int)HttpStatusCode.BadRequest;

                return result;
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
            ByteString parsedKey = ByteString.Parse(key ?? "");

            Record result = (await this.store.GetRecords(new[] { parsedKey })).First();

            return Json(new
            {
                key = parsedKey.ToString(),
                value = result.Value?.ToString(),
                version = result.Version.ToString()
            });
        }
    }
}
