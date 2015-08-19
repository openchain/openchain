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
                PermissionSet accountPermissions = await GetPermissions(signedAddresses, mutation.AccountKey.Account, mutation.AccountKey.Asset.FullPath);

                AccountStatus previousStatus = accounts[mutation.AccountKey];

                if (accountPermissions.AccountModify != Access.Permit)
                    throw new TransactionInvalidException("AccountModificationUnauthorized");

                if (mutation.Balance < previousStatus.Balance && accountPermissions.AccountNegative != Access.Permit)
                {
                    // Decreasing the balance
                    if (mutation.Balance >= 0)
                    {
                        // Spending existing funds
                        if (accountPermissions.AccountSpend != Access.Permit)
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
                PermissionSet dataRecordPermissions = await GetPermissions(signedAddresses, alias.Key.Path, alias.Key.Name);

                if (dataRecordPermissions.DataModify != Access.Permit)
                    throw new TransactionInvalidException("CannotModifyData");
            }
        }

        private async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> signedAddresses, LedgerPath path, string recordName)
        {
            PermissionSet accumulativePermissions = PermissionSet.DenyAll;

            for (int i = 0; i < path.Segments.Count; i++)
            {
                bool recursiveOnly = i != path.Segments.Count - 1;
                PermissionSet[] permissions = await Task.WhenAll(this.permissions.Select(item => item.GetPermissions(signedAddresses, path, recursiveOnly, recordName)));

                PermissionSet currentLevelPermissions = permissions
                    .Aggregate(PermissionSet.Unset, (previous, current) => previous.Add(current));

                accumulativePermissions = accumulativePermissions.AddLevel(currentLevelPermissions);
            }

            return accumulativePermissions;
        }
    }
}
