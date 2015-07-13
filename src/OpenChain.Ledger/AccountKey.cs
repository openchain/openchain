using OpenChain.Core;
using System;
using System.IO;

namespace OpenChain.Ledger
{
    public class AccountKey : BinaryValue
    {
        public AccountKey(string account, string asset)
            : base(BinaryValueUsage.AccountKey)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            this.Account = LedgerPath.Parse(account);
            this.Asset = LedgerPath.Parse(asset);
            this.SetBinaryData();
        }

        public LedgerPath Account { get; }

        public LedgerPath Asset { get; }

        protected override void Write(BinaryWriter writer)
        {
            writer.Write((int)Usage);
            writer.Write(Account.FullPath);
            writer.Write(Asset.FullPath);
        }
    }
}
