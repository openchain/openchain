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

            long amount;
            if (record.Value.Value.Count == 0)
                amount = 0;
            else if (record.Value.Value.Count == 8)
                amount = BitConverter.ToInt64(record.Value.Value.Reverse().ToArray(), 0);
            else
                throw new ArgumentOutOfRangeException(nameof(record));

            return new AccountStatus(new AccountKey(key.Path, key.AdditionalKeyComponents[0]), amount, record.Version);
        }

        public AccountKey AccountKey { get; }

        public long Balance { get; }

        public BinaryData Version { get; }
    }
}
