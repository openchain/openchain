using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenChain.Core
{
    public class Transaction
    {
        public Transaction(IEnumerable<AccountEntry> accountEntries, BinaryData metadata)
        {
            if (accountEntries == null)
                throw new ArgumentNullException(nameof(accountEntries));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.AccountEntries = new ReadOnlyCollection<AccountEntry>(accountEntries.ToList());
            this.Metadata = metadata;

            if (this.AccountEntries.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(accountEntries));
        }
        
        public IReadOnlyList<AccountEntry> AccountEntries { get; }

        public BinaryData Metadata { get; }
    }
}
