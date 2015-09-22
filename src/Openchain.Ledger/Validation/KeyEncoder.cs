// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Security.Cryptography;

namespace Openchain.Ledger.Validation
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
