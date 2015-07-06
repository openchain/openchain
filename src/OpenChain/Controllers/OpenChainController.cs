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
using Microsoft.AspNet.WebSockets.Server;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNet.Cors.Core;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace OpenChain.Controllers
{
    [EnableCors("Any")]
    [Route("")]
    public class OpenChainController : Controller
    {
        private readonly LedgerController controller;
        
        public OpenChainController()
        {
            ITransactionStore store = new SqliteTransactionStore(@"D:\Flavien\Documents\Visual Studio 2015\Projects\OpenChain\src\OpenChain.Console\ledger.db");
            this.controller = new LedgerController(store, new BasicValidator(store));
        }

        [HttpGet("transactionstream")]
        public async Task<ActionResult> GetStream(string from)
        {
            BinaryData ledgerRecordHash;
            if (string.IsNullOrEmpty(from))
                ledgerRecordHash = null;
            else
                ledgerRecordHash = BinaryData.Parse(from);

            IReadOnlyList<BinaryData> records = await this.controller.Store.GetTransactionStream(ledgerRecordHash);

            return Json(records.Select(record => new { raw = record.ToString() }).ToArray());

            ////TransactionStreamWebSocketHandler handler = new TransactionStreamWebSocketHandler(ledgerRecordHash);
            ////WebSocketMiddleware webSocket = new WebSocketMiddleware(handler.Process, new WebSocketOptions());

            ////await webSocket.Invoke(this.Request.HttpContext);

            //if (Context.IsWebSocketRequest)
            //{
            //    WebSocket webSocket = await this.Context.AcceptWebSocketAsync();
            //    ArraySegment<byte> segment = new ArraySegment<byte>(Encoding.UTF8.GetBytes("\"Hello world\""));
            //    await webSocket.SendAsync(segment, WebSocketMessageType.Text, false, CancellationToken.None);
            //}

            ////return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
            //return new HttpStatusCodeResult((int)HttpStatusCode.SwitchingProtocols);
        }

        // POST api/values
        [HttpPost("submit")]
        public async Task<JObject> Post([FromBody]string transaction)
        {
            JObject body = JObject.Parse(transaction);

            BinaryData parsedTransaction = BinaryData.Parse((string)body["raw"]);
            // Validate deserialization
            Transaction deserializedTransaction = TransactionSerializer.DeserializeTransaction(parsedTransaction.ToArray());

            BinaryData ledgerRecord = await this.controller.PostTransaction(parsedTransaction, null);

            return new JObject(new
            {
                ledger_record = ledgerRecord.ToString()
            });
        }
    }
}
