using System;
using OpenChain.Core;

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
            long value;
            try
            {
                key = BinaryValue.Read(mutation.Key) as AccountKey;
                if (mutation.Value == null)
                    value = 0;
                else
                    value = ((Int64Value)BinaryValue.Read(mutation.Value)).Value;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            if (key != null)
                return new AccountStatus(key, value, mutation.Version);
            else
                return null;
        }

        public AccountKey AccountKey { get; }

        public long Balance { get; }

        public BinaryData Version { get; }
    }
}
