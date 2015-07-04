using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenChain.Core;
using MongoDB.Bson.Serialization;

namespace OpenChain.Server
{
    public class BasicValidator : ITransactionValidator
    {
        private readonly ITransactionStore store;

        public BasicValidator(ITransactionStore store)
        {
            this.store = store;
        }

        public async Task<bool> IsValid(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            //BsonSerializer.Deserialize<LedgerRecordMetadata>(record.ExternalMetadata.Value.ToArray());
            IReadOnlyDictionary<AccountKey, AccountEntry> accounts =
                await this.store.GetAccounts(transaction.AccountEntries.Select(entry => entry.AccountKey));

            foreach (AccountEntry entry in transaction.AccountEntries)
            {
                LedgerPath accountPath;
                LedgerPath assetPath;
                if (!LedgerPath.TryParse(entry.AccountKey.Account, out accountPath) || !LedgerPath.TryParse(entry.AccountKey.Asset, out assetPath))
                    return false;

                if (entry.Version.Equals(BinaryData.Empty))
                    if (!await CheckCanCreate(transaction, authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        return false;

                if (entry.Amount > 0)
                {
                    if (!CheckCanReceive(transaction, authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        return false;
                }
                else if (entry.Amount < 0)
                {
                    if (!CheckCanSend(transaction, authentication, accountPath, assetPath, accounts[entry.AccountKey], entry))
                        return false;
                }
            }

            return true;
        }

        private bool CheckCanSend(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
        {
            if (currentState.Amount + proposedChange.Amount < 0)
                return false;
            else
                return true;
        }

        private bool CheckCanReceive(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
        {
            return !accountPath.IsDirectory;
        }

        private async Task<bool> CheckCanCreate(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication, LedgerPath accountPath, LedgerPath assetPath, AccountEntry currentState, AccountEntry proposedChange)
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
