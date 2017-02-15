using NBitcoin;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Openchain.SDK
{
    public class MutationSigner
    {
        public ByteString PublicKey { get; }
        
        private readonly ExtKey _key;

        public MutationSigner(ExtKey key)
        {
            _key = key;

            PublicKey = ByteString.Parse(_key.Neuter().PubKey.ToHex());
        }

        public ByteString Sign(ByteString mutation)
        {
            //var transactionBuffer = new ByteString(mutation.ToByteArray());
            
            var hash = MessageSerializer.ComputeHash(mutation.ToByteArray());

            var hashBuffer = new uint256(hash);

            var signature = _key.PrivateKey.Sign(hashBuffer).ToDER();

            var signatureBuffer = new ByteString(signature);
            
            return signatureBuffer;
        }
    }
}
