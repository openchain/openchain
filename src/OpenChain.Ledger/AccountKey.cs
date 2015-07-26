using System;

namespace OpenChain.Ledger
{
    public class AccountKey : IEquatable<AccountKey>
    {
        public AccountKey(LedgerPath account, LedgerPath asset)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            this.Account = account;
            this.Asset = asset;
            this.Key = new RecordKey(RecordType.Account, account, new[] { asset });
        }

        public static AccountKey Parse(string account, string asset)
        {
            return new AccountKey(
                LedgerPath.Parse(account),
                LedgerPath.Parse(asset));
        }

        public LedgerPath Account { get; }

        public LedgerPath Asset { get; }

        public RecordKey Key { get; }

        public bool Equals(AccountKey other)
        {
            if (other == null)
                return false;
            else
                return StringComparer.Ordinal.Equals(Key.ToString(), other.Key.ToString());
        }

        public override bool Equals(object obj)
        {
            if (obj is AccountKey)
                return this.Equals((AccountKey)obj);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(ToString());
        }
    }
}
