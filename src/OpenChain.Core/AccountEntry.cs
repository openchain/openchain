using System;

namespace OpenChain.Core
{
    public class AccountEntry
    {
        public AccountEntry(AccountKey accountKey, long amount, BinaryData version)
        {
            if (accountKey == null)
                throw new ArgumentNullException(nameof(accountKey));

            if (version == null)
                throw new ArgumentNullException(nameof(version));

            this.AccountKey = accountKey;
            this.Amount = amount;
            this.Version = version;
        }

        public AccountKey AccountKey { get; }

        public long Amount { get; }

        public BinaryData Version { get; }
    }
}
