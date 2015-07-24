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

        public static AccountStatus FromRecord(Record mutation)
        {
            AccountKey key;
            long value;
            try
            {
                key = BinaryValue.Read(mutation.Key, isKey: true) as AccountKey;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }

            // This record does not represent an account key
            if (key == null)
                return null;

            // If the value is unset, the balance is 0
            if (mutation.Value.Value.Count == 0)
                value = 0;
            else
                value = ((Int64Value)BinaryValue.Read(mutation.Value, isKey: false)).Value;

            return new AccountStatus(key, value, mutation.Version);
        }

        public AccountKey AccountKey { get; }

        public long Balance { get; }

        public BinaryData Version { get; }
    }
}
