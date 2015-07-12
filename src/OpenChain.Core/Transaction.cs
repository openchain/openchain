using System;

namespace OpenChain.Core
{
    public class Transaction
    {
        public Transaction(BinaryData mutationSet, DateTime timestamp, BinaryData externalMetadata)
        {
            if (mutationSet == null)
                throw new ArgumentNullException(nameof(mutationSet));

            if (externalMetadata == null)
                throw new ArgumentNullException(nameof(externalMetadata));

            this.MutationSet = mutationSet;
            this.Timestamp = timestamp;
            this.ExternalMetadata = externalMetadata;
        }

        public BinaryData MutationSet { get; }

        public DateTime Timestamp { get; }

        public BinaryData ExternalMetadata { get; }
    }
}
