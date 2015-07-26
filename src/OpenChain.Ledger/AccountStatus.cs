using System;
using System.Linq;

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

        public static AccountStatus FromRecord(RecordKey key, Record record)
        {
            if (key.RecordType != RecordType.Account)
                throw new ArgumentOutOfRangeException(nameof(key));

            if (record.Value.Value.Count != 4)
                throw new ArgumentOutOfRangeException(nameof(record));

            return new AccountStatus(
                new AccountKey(key.Path, key.AdditionalKeyComponents[0]),
                BitConverter.ToInt64(record.Value.Value.Reverse().ToArray(), 0),
                record.Version);
        }

        public AccountKey AccountKey { get; }

        public long Balance { get; }

        public BinaryData Version { get; }
    }
}
