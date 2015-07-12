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

            this.Account = account;
            this.Asset = asset;
            this.SetBinaryData();
        }

        public string Account { get; }

        public string Asset { get; }

        protected override void Write(BinaryWriter writer)
        {
            writer.Write((int)Usage);
            writer.Write(Account);
            writer.Write(Asset);
        }
    }
}
