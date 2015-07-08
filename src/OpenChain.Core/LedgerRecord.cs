using System;

namespace OpenChain.Core
{
    public class LedgerRecord
    {
        public LedgerRecord(BinaryData transaction, DateTime timestamp, BinaryData externalMetadata, BinaryData previousRecordHash)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (externalMetadata == null)
                throw new ArgumentNullException(nameof(externalMetadata));

            if (previousRecordHash == null)
                throw new ArgumentNullException(nameof(previousRecordHash));

            this.Transaction = transaction;
            this.Timestamp = timestamp;
            this.ExternalMetadata = externalMetadata;
            this.PreviousRecordHash = previousRecordHash;
        }

        public BinaryData Transaction { get; }

        public DateTime Timestamp { get; }

        public BinaryData ExternalMetadata { get; }

        public BinaryData PreviousRecordHash { get; }
    }
}
