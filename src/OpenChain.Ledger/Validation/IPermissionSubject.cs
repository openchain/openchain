using System.Collections.Generic;

namespace OpenChain.Ledger.Validation
{
    public interface IPermissionSubject
    {
        bool IsMatch(IReadOnlyList<SignatureEvidence> authentication);
    }
}
