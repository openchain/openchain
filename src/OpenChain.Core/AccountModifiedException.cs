using System;

namespace OpenChain.Core
{
    public class AccountModifiedException : Exception
    {
        public AccountModifiedException(AccountEntry failedEntry)
            : base(string.Format(
                "Version '{0}' of account '{1}' for asset '{2}' no longer exists.",
                failedEntry.Version,
                failedEntry.AccountKey.Account, 
                failedEntry.AccountKey.Asset))
        {
            this.FailedEntry = failedEntry;
        }

        public AccountEntry FailedEntry { get; }
    }
}
