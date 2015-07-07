using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class TransactionStreamWebSocketMiddleware
    {
        private readonly RequestDelegate next;
        private readonly BinaryData lastLedgerRecordHash;

        public TransactionStreamWebSocketMiddleware(RequestDelegate next, BinaryData lastLedgerRecordHash)
        {
            this.next = next;
            this.lastLedgerRecordHash = lastLedgerRecordHash;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                Console.WriteLine("Echo: " + context.Request.Path);
                var webSocket = await context.AcceptWebSocketAsync();
                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("\"Hello World\"")), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                return;
            }
            else
            {
                await this.next(context);
            }
        }
    }
}
