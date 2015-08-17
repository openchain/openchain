using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class OpenLoopValidator : IMutationValidator
    {
        private readonly IList<IPermissionsProvider> permissions;

        public OpenLoopValidator(IList<IPermissionsProvider> permissions)
        {
            this.permissions = permissions;
        }

        public async Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
        {
            await ValidateAccountMutations(mutation.AccountMutations, accounts, authentication);
            await ValidateDataMutations(mutation.DataRecords, authentication);
        }

        private async Task ValidateAccountMutations(
            IReadOnlyList<AccountStatus> accountMutations,
            IReadOnlyDictionary<AccountKey, AccountStatus> accounts,
            IReadOnlyList<SignatureEvidence> signedAddresses)
        {
            foreach (AccountStatus mutation in accountMutations)
            {
                PermissionSet assetPermissions = await GetPermissions(signedAddresses, mutation.AccountKey.Asset);
                PermissionSet accountPermissions = await GetPermissions(signedAddresses, mutation.AccountKey.Account);

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


        private async Task ValidateDataMutations(
            IReadOnlyList<KeyValuePair<RecordKey, ByteString>> aliases,
            IReadOnlyList<SignatureEvidence> signedAddresses)
        {
            foreach (KeyValuePair<RecordKey, ByteString> alias in aliases)
            {
                PermissionSet dataRecordPermissions = await GetPermissions(signedAddresses, alias.Key.Path);

                if (!dataRecordPermissions.ModifyData)
                    throw new TransactionInvalidException("CannotModifyData");

                if (alias.Key.Name == "acl" && !dataRecordPermissions.ModifyPermissions)
                    throw new TransactionInvalidException("CannotModifyPermissions");
            }
        }

        private async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> signedAddresses, LedgerPath asset)
        {
            IList<PermissionSet> permissions = await Task.WhenAll(this.permissions.Select(item => item.GetPermissions(signedAddresses, asset)));
            return permissions.Aggregate(PermissionSet.AllowAll, (accumulator, value) => accumulator.Intersect(value), result => result);
        }
    }
}
