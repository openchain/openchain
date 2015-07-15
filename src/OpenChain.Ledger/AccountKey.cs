using System;
using System.IO;
using System.Text;

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
            byte[] account = Encoding.UTF8.GetBytes(Account.FullPath);
            byte[] asset = Encoding.UTF8.GetBytes(Asset.FullPath);
            writer.Write((int)Usage);
            writer.Write((uint)account.Length);
            writer.Write(account);
            writer.Write((uint)asset.Length);
            writer.Write(asset);
        }
    }
}
