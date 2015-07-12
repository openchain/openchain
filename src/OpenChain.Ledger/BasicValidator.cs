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

        public async Task Validate(IReadOnlyList<AccountEntry> accountEntries, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            //BsonSerializer.Deserialize<LedgerRecordMetadata>(record.ExternalMetadata.Value.ToArray());
            IReadOnlyDictionary<AccountKey, AccountEntry> accounts =
                await this.store.GetAccounts(accountEntries.Select(entry => entry.AccountKey));

            foreach (AccountEntry entry in accountEntries)
            {
                LedgerPath accountPath;
                LedgerPath assetPath;
                if (!LedgerPath.TryParse(entry.AccountKey.Account, out accountPath) || !LedgerPath.TryParse(entry.AccountKey.Asset, out assetPath))
                    throw new TransactionInvalidException("InvalidPathFormat");

                if (entry.Version.Equals(BinaryData.Empty))
                    if (!await CheckCanCreate(authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        throw new TransactionInvalidException("AccountCannotBeCreated");

                if (entry.Amount > 0)
                {
                    if (!CheckCanReceive(authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        throw new TransactionInvalidException("AccountCannotReceive");
                }
                else if (entry.Amount < 0)
                {
                    if (!CheckCanSend(authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        throw new TransactionInvalidException("AccountCannotSend");
                }
            }
        }

        private bool CheckCanSend(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
        {
            if (currentState.Amount + proposedChange.Amount < 0)
                return false;
            else
                return true;
        }

        private bool CheckCanReceive(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
        {
            return !accountPath.IsDirectory;
        }

        private async Task<bool> CheckCanCreate(IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
        {
            if (accountPath.Segments.Count < 3)
                return false;

            if (accountPath.Segments[0] != "account" || accountPath.Segments[1] != "p2pkh")
                return false;

            LedgerPath rootPath = LedgerPath.FromSegments(new[] { accountPath.Segments[0], accountPath.Segments[1], accountPath.Segments[2] }, true);

            AccountEntry parentAccount = (await this.store.GetAccounts(new[] { new AccountKey(rootPath.FullPath, currentState.AccountKey.Asset) })).First().Value;
            if (parentAccount.Version.Equals(BinaryData.Empty))
                return false;

            return true;
        }
    }
}
