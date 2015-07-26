using System;

namespace OpenChain.Ledger
{
    public class AccountKey
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
    }
}
