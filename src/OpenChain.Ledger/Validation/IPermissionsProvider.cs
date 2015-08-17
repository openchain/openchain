using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public interface IPermissionsProvider
    {
        Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path);
    }
}
