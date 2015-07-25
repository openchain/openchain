namespace OpenChain.Ledger
{
    public class SignatureEvidence
    {
        public SignatureEvidence(BinaryData publicKey, BinaryData signature)
        {
            this.PublicKey = publicKey;
            this.Signature = signature;
        }

        public BinaryData PublicKey { get; private set; }

        public BinaryData Signature { get; private set; }
    }
}
