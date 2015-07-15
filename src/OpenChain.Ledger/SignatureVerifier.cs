using System;
using System.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;

namespace OpenChain.Ledger
{
    public class ECKey
    {
        public static X9ECParameters Secp256k1 { get; } = SecNamedCurves.GetByName("secp256k1");

        public static ECDomainParameters DomainParameter { get; } = new ECDomainParameters(Secp256k1.Curve, Secp256k1.G, Secp256k1.N, Secp256k1.H);

        private ECPublicKeyParameters key;

        public ECKey(byte[] vch)
        {
            var q = Secp256k1.Curve.DecodePoint(vch);
            key = new ECPublicKeyParameters("EC", q, DomainParameter);
        }

        public bool VerifySignature(byte[] hash, byte[] signature)
        {
            ECDsaSigner signer = new ECDsaSigner();
            ECDSASignature parsedSignature = ECDSASignature.FromDER(signature);
            signer.Init(false, key);
            return signer.VerifySignature(hash, parsedSignature.R, parsedSignature.S);
        }

        private class ECDSASignature
        {
            public BigInteger R { get; }

            public BigInteger S { get; }

            public ECDSASignature(BigInteger r, BigInteger s)
            {
                R = r;
                S = s;
            }

            public static ECDSASignature FromDER(byte[] sig)
            {
                try
                {
                    Asn1InputStream decoder = new Asn1InputStream(sig);
                    var seq = decoder.ReadObject() as DerSequence;
                    if (seq == null || seq.Count != 2)
                        throw new FormatException("Invalid DER signature");

                    return new ECDSASignature(((DerInteger)seq[0]).Value, ((DerInteger)seq[1]).Value);
                }
                catch (IOException ex)
                {
                    throw new FormatException("Invalid DER signature", ex);
                }
            }
        }
    }
}
