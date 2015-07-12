using OpenChain.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class BasicValidator : IRulesValidator
    {
        private readonly ILedgerQueries store;

        public BasicValidator(ILedgerQueries store)
        {
            this.store = store;
        }

        public async Task Validate(IReadOnlyList<AccountStatus> accountEntries, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            //BsonSerializer.Deserialize<LedgerRecordMetadata>(record.ExternalMetadata.Value.ToArray());
            IReadOnlyDictionary<AccountKey, AccountStatus> accounts =
                await this.store.GetAccounts(accountEntries.Select(entry => entry.AccountKey));

            foreach (AccountStatus account in accountEntries)
            {
                LedgerPath accountPath;
                LedgerPath assetPath;
                if (!LedgerPath.TryParse(account.AccountKey.Account, out accountPath) || !LedgerPath.TryParse(account.AccountKey.Asset, out assetPath))
                    throw new TransactionInvalidException("InvalidPathFormat");

                if (account.Version.Equals(BinaryData.Empty))
                    if (!await CheckCanCreate(authentication, accountPath, assetPath, accounts[account.AccountKey], account))
                        throw new TransactionInvalidException("AccountCannotBeCreated");

                if (account.Balance > 0)
                {
                    if (!CheckCanReceive(authentication, accountPath, assetPath, accounts[account.AccountKey], account))
                        throw new TransactionInvalidException("AccountCannotReceive");
                }
                else if (account.Balance < 0)
                {
                    if (!CheckCanSend(authentication, accountPath, assetPath, accounts[account.AccountKey], account))
                        throw new TransactionInvalidException("AccountCannotSend");
                }
            }
        }

        private bool CheckCanSend(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountStatus currentState, AccountStatus proposedChange)
        {
            if (currentState.Balance + proposedChange.Balance < 0)
                return false;
            else
                return true;
        }

        private bool CheckCanReceive(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountStatus currentState, AccountStatus proposedChange)
        {
            return !accountPath.IsDirectory;
        }

        private async Task<bool> CheckCanCreate(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountStatus currentState, AccountStatus proposedChange)
        {
            if (accountPath.Segments.Count < 3)
                return false;

            if (accountPath.Segments[0] != "account" || accountPath.Segments[1] != "p2pkh")
                return false;

            LedgerPath rootPath = LedgerPath.FromSegments(new[] { accountPath.Segments[0], accountPath.Segments[1], accountPath.Segments[2] }, true);

            AccountStatus parentAccount = (await this.store.GetAccounts(new[] { new AccountKey(rootPath.FullPath, currentState.AccountKey.Asset) })).First().Value;
            if (parentAccount.Version.Equals(BinaryData.Empty))
                return false;

            return true;
        }
    }
}
