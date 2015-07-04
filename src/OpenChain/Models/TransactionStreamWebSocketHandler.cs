using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class TransactionStreamWebSocketHandler
    {
        private readonly BinaryData lastLedgerRecordHash;

        public TransactionStreamWebSocketHandler(BinaryData lastLedgerRecordHash)
        {
            this.lastLedgerRecordHash = lastLedgerRecordHash;
        }

        public async Task Process(HttpContext context)
        {
            
        }
    }
}
