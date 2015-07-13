using OpenChain.Core;
using System;

namespace OpenChain.Ledger
{
    public class AccountStatus
    {
        public AccountStatus(AccountKey accountKey, long amount, BinaryData version)
        {
            if (accountKey == null)
                throw new ArgumentNullException(nameof(accountKey));

            if (version == null)
                throw new ArgumentNullException(nameof(version));

            this.AccountKey = accountKey;
            this.Balance = amount;
            this.Version = version;
        }

        public static AccountStatus FromKeyValuePair(KeyValuePair mutation)
        {
            AccountKey key;
            Int64Value value;
            try
            {
                key = BinaryValue.Read(mutation.Key) as AccountKey;
                value = BinaryValue.Read(mutation.Value) as Int64Value;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            if (key != null || value != null)
                return new AccountStatus(key, value.Value, mutation.Version);
            else
                return null;
        }

        public AccountKey AccountKey { get; }

        public long Balance { get; }

        public BinaryData Version { get; }
    }
}
