using OpenChain.Core;
using System;

namespace OpenChain.Ledger
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

        public static AccountEntry FromMutation(Mutation mutation)
        {
            AccountKey key = BinaryValue.Read(mutation.Key) as AccountKey;
            Int64Value value = BinaryValue.Read(mutation.Value) as Int64Value;

            if (key != null || value != null)
                return new AccountEntry(key, value.Value, mutation.Version);
            else
                return null;
        }

        public AccountKey AccountKey { get; }

        public long Amount { get; }

        public BinaryData Version { get; }
    }
}
