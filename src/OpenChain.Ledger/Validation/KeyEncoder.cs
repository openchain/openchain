using System;
using System.Linq;
using System.Security.Cryptography;

namespace OpenChain.Ledger.Validation
{
    public class KeyEncoder
    {
        private readonly byte versionByte;

        public KeyEncoder(byte versionByte)
        {
            this.versionByte = versionByte;
        }

        public bool IsP2pkh(string address)
        {
            try
            {
                byte[] pubKeyHash = Base58CheckEncoding.Decode(address);
                if (pubKeyHash.Length != 21 || pubKeyHash[0] != versionByte)
                    return false;
                else
                    return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public string GetPubKeyHash(ByteString pubKey)
        {
            Org.BouncyCastle.Crypto.Digests.RipeMD160Digest ripe = new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] shaResult = sha256.ComputeHash(pubKey.ToByteArray());
                ripe.BlockUpdate(shaResult, 0, shaResult.Length);
                byte[] hash = new byte[20];
                ripe.DoFinal(hash, 0);
                return Base58CheckEncoding.Encode(new byte[] { versionByte }.Concat(hash).ToArray());
            }
        }
    }
}
