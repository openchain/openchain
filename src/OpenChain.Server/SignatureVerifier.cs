using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public class SignatureVerifier
    {
        //public static bool VerifySignature(byte[] publicKey, byte[] msgBytes, byte[] sigBytes)
        //{
        //    int first = (sigBytes[0] - 27);
        //    bool comp = (first & 4) != 0;
        //    int rec = first & 3;

        //    BigInteger[] sig = ParseSig(sigBytes, 1);
        //    byte[] msgHash = DigestUtilities.CalculateDigest("SHA-256", DigestUtilities.CalculateDigest("SHA-256", msgBytes));

        //    ECPoint Q = Recover(msgHash, sig, rec, true);

        //    byte[] qEnc = Q.GetEncoded(comp);
        //    Console.WriteLine("Q: " + Hex.ToHexString(qEnc));

        //    byte[] qHash = DigestUtilities.CalculateDigest("RIPEMD-160", DigestUtilities.CalculateDigest("SHA-256", qEnc));
        //    Console.WriteLine("RIPEMD-160(SHA-256(Q)): " + Hex.ToHexString(qHash));

        //    Console.WriteLine("Signature verified correctly: " + VerifySignature(Q, msgHash, sig));
        //}

        //public static void CheckSignedMessage(string message, string sig64)
        //{
        //    byte[] sigBytes = Convert.FromBase64String(sig64);
        //    byte[] msgBytes = FormatMessageForSigning(message);


        //}
    }
}
