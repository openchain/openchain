using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class OpenLoopValidator : IMutationValidator
    {
        private readonly IPermissionsProvider permissions;

        public OpenLoopValidator(IPermissionsProvider permissions)
        {
            this.permissions = permissions;
        }

        public async Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
        {
            await ValidateAccountMutations(mutation.AccountMutations, accounts, authentication);
            await ValidateAssetDefinitionMutations(mutation.AssetDefinitions, authentication);
            await ValidateDataMutations(mutation.DataRecords, authentication);
        }

        private async Task ValidateAccountMutations(
            IReadOnlyList<AccountStatus> accountMutations,
            IReadOnlyDictionary<AccountKey, AccountStatus> accounts,
            IReadOnlyList<SignatureEvidence> signedAddresses)
        {
            foreach (AccountStatus mutation in accountMutations)
            {
                PermissionSet assetPermissions = await this.permissions.GetPermissions(signedAddresses, mutation.AccountKey.Asset);
                PermissionSet accountPermissions = await this.permissions.GetPermissions(signedAddresses, mutation.AccountKey.Account);

                AccountStatus previousStatus = accounts[mutation.AccountKey];

                if (!accountPermissions.AffectBalance)
                    throw new TransactionInvalidException("AccountModificationUnauthorized");

                if (mutation.Balance < previousStatus.Balance && !assetPermissions.Issuance)
                {
                    // Decreasing the balance
                    if (mutation.Balance >= 0)
                    {
                        // Spending existing funds
                        if (!accountPermissions.SpendFrom)
                            throw new TransactionInvalidException("CannotSpendFromAccount");
                    }
                    else
                    {
                        // Spending non-existing funds
                        throw new TransactionInvalidException("CannotIssueAsset");
                    }
                }
            }
        }

        private async Task ValidateAssetDefinitionMutations(
            IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations,
            IReadOnlyList<SignatureEvidence> signedAddresses)
        {
            foreach (KeyValuePair<LedgerPath, string> mutation in assetDefinitionMutations)
            {
                PermissionSet assetPermissions = await this.permissions.GetPermissions(signedAddresses, mutation.Key);

                if (!assetPermissions.Issuance)
                    throw new TransactionInvalidException("CannotSpendFromAccount");
            }
        }

        private async Task ValidateDataMutations(
            IReadOnlyList<KeyValuePair<LedgerPath, BinaryData>> aliases,
            IReadOnlyList<SignatureEvidence> signedAddresses)
        {
            foreach (KeyValuePair<LedgerPath, BinaryData> alias in aliases)
            {
                PermissionSet aliasPermissions = await this.permissions.GetPermissions(signedAddresses, alias.Key);

                if (!aliasPermissions.ModifyData)
                    throw new TransactionInvalidException("CannotModifyAlias");
            }
        }
    }
}
