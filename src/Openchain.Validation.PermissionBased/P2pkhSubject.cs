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
using System.Collections.Generic;
using System.Linq;
using Openchain.Ledger;

namespace Openchain.Validation.PermissionBased
{
    /// <summary>
    /// Represents an implementation of the <see cref="IPermissionSubject"/> interface based on public key hashes.
    /// </summary>
    public class P2pkhSubject : IPermissionSubject
    {
        private readonly KeyEncoder keyEncoder;

        public P2pkhSubject(IEnumerable<string> addresses, int signaturesRequired, KeyEncoder keyEncoder)
        {
            this.Addresses = addresses.ToList().AsReadOnly();
            this.SignaturesRequired = signaturesRequired;
            this.keyEncoder = keyEncoder;
        }

        /// <summary>
        /// Gets the list of valid addresses.
        /// </summary>
        public IReadOnlyList<string> Addresses { get; }

        /// <summary>
        /// Gets the number of required signatures for a match.
        /// </summary>
        public int SignaturesRequired { get; }

        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication)
        {
            HashSet<string> identities = new HashSet<string>(authentication.Select(evidence => keyEncoder.GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);
            return Addresses.Count(address => identities.Contains(address)) >= SignaturesRequired;
        }
    }
}
