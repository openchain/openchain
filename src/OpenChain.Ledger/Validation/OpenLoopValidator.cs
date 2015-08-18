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

                if (!accountPermissions.AccountModify)
                    throw new TransactionInvalidException("AccountModificationUnauthorized");

                if (mutation.Balance < previousStatus.Balance && !accountPermissions.AccountNegative)
                {
                    // Decreasing the balance
                    if (mutation.Balance >= 0)
                    {
                        // Spending existing funds
                        if (!accountPermissions.AccountSpend)
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

                if (!dataRecordPermissions.DataModify)
                    throw new TransactionInvalidException("CannotModifyData");
            }
        }

        private async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> signedAddresses, LedgerPath asset, string recordName)
        {
            IList<PermissionSet> permissions = await Task.WhenAll(this.permissions.Select(item => item.GetPermissions(signedAddresses, asset, recordName)));
            return permissions.Aggregate(PermissionSet.AllowAll, (accumulator, value) => accumulator.Intersect(value), result => result);
        }
    }
}
