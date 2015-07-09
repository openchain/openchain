using System;

namespace OpenChain.Core
{
    public class LedgerRecord
    {
        public LedgerRecord(BinaryData transaction, DateTime timestamp, BinaryData externalMetadata)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (externalMetadata == null)
                throw new ArgumentNullException(nameof(externalMetadata));

            this.Transaction = transaction;
            this.Timestamp = timestamp;
            this.ExternalMetadata = externalMetadata;
        }

        public BinaryData Transaction { get; }

        public DateTime Timestamp { get; }

        public BinaryData ExternalMetadata { get; }
    }
}
