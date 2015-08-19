using System.Collections.Generic;

namespace OpenChain.Ledger.Validation
{
    public class EveryoneSubject : IPermissionSubject
    {
        public static EveryoneSubject Instance { get; } = new EveryoneSubject();

        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication)
        {
            return true;
        }
    }
}
