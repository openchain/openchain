using System;
using System.IO;
using System.Text;

namespace OpenChain.Ledger
{
    public class AccountKey : BinaryValue
    {
        public AccountKey(BinaryValueUsage usage, string account, string asset)
            : base(usage)
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

        public override BinaryValueType Type => BinaryValueType.StringPair;

        protected override void Write(BinaryWriter writer)
        {
            byte[] account = Encoding.UTF8.GetBytes(Account.FullPath);
            byte[] asset = Encoding.UTF8.GetBytes(Asset.FullPath);
            writer.Write((uint)account.Length);
            writer.Write(account);
            writer.Write((uint)asset.Length);
            writer.Write(asset);
        }
    }
}
