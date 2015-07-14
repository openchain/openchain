using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenChain.Ledger
{
    public class TransactionMetadata
    {
        public TransactionMetadata(IEnumerable<SignatureEvidence> signatures)
        {
            this.Signatures = new ReadOnlyCollection<SignatureEvidence>(signatures.ToList());
        }

        public IReadOnlyList<SignatureEvidence> Signatures { get; }
    }
}
