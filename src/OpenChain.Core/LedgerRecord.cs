using System;

namespace OpenChain.Core
{
    public class LedgerRecord
    {
        public LedgerRecord(BinaryData payload, DateTime timestamp, BinaryData externalMetadata, BinaryData previousRecordHash)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (externalMetadata == null)
                throw new ArgumentNullException(nameof(externalMetadata));

            if (previousRecordHash == null)
                throw new ArgumentNullException(nameof(previousRecordHash));

            this.Payload = payload;
            this.Timestamp = timestamp;
            this.ExternalMetadata = externalMetadata;
            this.PreviousRecordHash = previousRecordHash;
        }

        public BinaryData Payload { get; }

        public DateTime Timestamp { get; }

        public BinaryData ExternalMetadata { get; }

        public BinaryData PreviousRecordHash { get; }
    }
}
