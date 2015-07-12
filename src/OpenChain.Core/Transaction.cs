using System;

namespace OpenChain.Core
{
    public class Transaction
    {
        public Transaction(BinaryData mutationSet, DateTime timestamp, BinaryData transactionMetadata)
        {
            if (mutationSet == null)
                throw new ArgumentNullException(nameof(mutationSet));

            if (transactionMetadata == null)
                throw new ArgumentNullException(nameof(transactionMetadata));

            this.MutationSet = mutationSet;
            this.Timestamp = timestamp;
            this.TransactionMetadata = transactionMetadata;
        }

        public BinaryData MutationSet { get; }

        public DateTime Timestamp { get; }

        public BinaryData TransactionMetadata { get; }
    }
}
