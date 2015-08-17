using System.Collections.Generic;
using System.Linq;

namespace OpenChain.Ledger.Validation
{
    public class PathPermissions
    {
        public PathPermissions(LedgerPath path, PermissionSet permissions, IEnumerable<string> identities)
        {
            this.Path = path;
            this.Permissions = permissions;
            this.Identities = identities.ToList().AsReadOnly();
        }

        public LedgerPath Path { get; }

        public PermissionSet Permissions { get; }

        public IReadOnlyList<string> Identities { get; }
    }
}
