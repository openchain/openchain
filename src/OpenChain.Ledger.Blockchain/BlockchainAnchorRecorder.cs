using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Blockchain
{
    public class BlockchainAnchorRecorder : IAnchorRecorder
    {
        private readonly byte[] anchorMarker = new byte[] { 0x4f, 0x43 };

        public BlockchainAnchorRecorder()
        {
        }

        public async Task RecordAnchor(LedgerAnchor anchor)
        {
            byte[] anchorPayload =
                anchorMarker
                .Concat(BitConverter.GetBytes(anchor.TransactionCount).Reverse())
                .Concat(anchor.FullStoreHash.ToByteArray())
                .ToArray();
        }
    }
}
