using System;

namespace OpenChain.Core
{
    public class AccountKey : IEquatable<AccountKey>
    {
        public AccountKey(string account, string asset)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            this.Account = account;
            this.Asset = asset;
        }

        public string Account { get; }

        public string Asset { get; }

        public bool Equals(AccountKey other)
        {
            if (other == null)
                return false;
            else
                return this.Account.Equals(other.Account) && this.Asset.Equals(other.Asset);
        }

        public override int GetHashCode()
        {
            return this.Account.GetHashCode() ^ this.Asset.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as AccountKey);
        }
    }
}
