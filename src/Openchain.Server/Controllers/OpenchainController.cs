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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;
using Openchain.Ledger;
using Openchain.Ledger.Validation;

namespace Openchain.Server.Controllers
{
    [EnableCors("Any")]
    [Route("")]
    public class OpenchainController : Controller
    {
        private readonly ITransactionStore store;
        private readonly ILogger logger;

        public OpenchainController(ITransactionStore store, ILogger logger)
        {
            this.store = store;
            this.logger = logger;
        }

        /// <summary>
        /// Submit a transaction for validation.
        /// </summary>
        /// <param name="body">
        /// The JSON object in the request body.
        /// Expected format:
        /// {
        ///   "mutation": "&lt;string>",
        ///   "signatures": [
        ///     {
        ///       "pub_key": "&lt;string>",
        ///       "signature": "&lt;string>"
        ///     },
        ///     ...
        ///   ]
        /// }
        /// </param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [HttpPost("submit")]
        public async Task<ActionResult> Post([FromBody]JObject body)
        {
            TransactionValidator validator = Context.ApplicationServices.GetService<TransactionValidator>();
            if (validator == null)
                return CreateErrorResponse("ValidationDisabled");

            ByteString parsedTransaction = ByteString.Parse((string)body["mutation"]);

            List<SignatureEvidence> authentication = new List<SignatureEvidence>();

            foreach (JObject evidence in body["signatures"])
            {
                authentication.Add(new SignatureEvidence(
                    ByteString.Parse((string)evidence["pub_key"]),
                    ByteString.Parse((string)evidence["signature"])));
            }

            ByteString transactionId;
            try
            {
                transactionId = await validator.PostTransaction(parsedTransaction, authentication);
            }
            catch (TransactionInvalidException exception)
            {
                logger.LogInformation("Rejected transaction: {0}", exception.Message);

                return CreateErrorResponse(exception.Reason);
            }

            logger.LogInformation("Validated transaction {0}", transactionId.ToString());

            return Json(new
            {
                transaction_hash = transactionId.ToString()
            });
        }

        private ActionResult CreateErrorResponse(string reason)
        {
            JsonResult result = Json(new
            {
                error_code = reason
            });

            result.StatusCode = (int)HttpStatusCode.BadRequest;

            return result;
        }

        [HttpGet("record")]
        public async Task<ActionResult> GetValue(string key)
        {
            ByteString parsedKey;

            try
            {
                parsedKey = ByteString.Parse(key ?? "");
            }
            catch (FormatException)
            {
                return new HttpStatusCodeResult(400);
            }

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
