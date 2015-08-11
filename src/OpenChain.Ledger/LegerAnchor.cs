using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class LedgerAnchor
    {
        public LedgerAnchor(ByteString position, ByteString fullStoreHash, long transactionCount)
        {
            Position = position;
            FullStoreHash = fullStoreHash;
            TransactionCount = transactionCount;
        }

        public ByteString Position { get; }

        public ByteString FullStoreHash { get; }

        public long TransactionCount { get; }
    }
}
