using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class StaticPermissionLayout : IPermissionsProvider
    {
        private readonly IList<Acl> permissions;
        private readonly KeyEncoder keyEncoder;

        public StaticPermissionLayout(IList<Acl> permissions, KeyEncoder keyEncoder)
        {
            this.permissions = permissions;
            this.keyEncoder = keyEncoder;
        }

        public Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, bool recursiveOnly, string recordName)
        {
            PermissionSet currentPermissions = PermissionSet.Unset;

            foreach (Acl acl in permissions)
            {
                if (acl.IsMatch(authentication, path, recursiveOnly, recordName))
                    currentPermissions = currentPermissions.Add(acl.Permissions);
            }

            return Task.FromResult(currentPermissions);
        }
    }
}
