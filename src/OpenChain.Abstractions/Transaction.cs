using System;

namespace OpenChain
{
    public class Transaction
    {
        public Transaction(BinaryData mutation, DateTime timestamp, BinaryData transactionMetadata)
        {
            if (mutation == null)
                throw new ArgumentNullException(nameof(mutation));

            if (transactionMetadata == null)
                throw new ArgumentNullException(nameof(transactionMetadata));

            this.Mutation = mutation;
            this.Timestamp = timestamp;
            this.TransactionMetadata = transactionMetadata;
        }

        public BinaryData Mutation { get; }

        public DateTime Timestamp { get; }

        public BinaryData TransactionMetadata { get; }
    }
}
