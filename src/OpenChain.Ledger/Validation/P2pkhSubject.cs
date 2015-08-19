using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenChain.Ledger.Validation
{
    public class P2pkhSubject : IPermissionSubject
    {
        private readonly KeyEncoder keyEncoder;

        public P2pkhSubject(IEnumerable<string> addresses, int signaturesRequired, KeyEncoder keyEncoder)
        {
            this.Addresses = addresses.ToList().AsReadOnly();
            this.SignaturesRequired = signaturesRequired;
            this.keyEncoder = keyEncoder;
        }

        public IReadOnlyList<string> Addresses { get; }

        public int SignaturesRequired { get; }

        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication)
        {
            HashSet<string> identities = new HashSet<string>(authentication.Select(evidence => keyEncoder.GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);
            return Addresses.Count(address => identities.Contains(address)) >= SignaturesRequired;
        }
    }
}
