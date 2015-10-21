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
using System.Threading.Tasks;

namespace Openchain.Ledger.Validation
{
    /// <summary>
    /// Represents the implicit permission layout where account names contain identities.
    /// Permissions are set for:
    /// - /asset/p2pkh/[addr]/ (AccountModify and optionally AccountSpend and DataModify if the record name
    ///   matches a third-party asset owned by the current identity)
    /// - / (AccountNegative if the record name matches a third-party asset owned by the current identity)
    /// </summary>
    public class P2pkhIssuanceImplicitLayout : IPermissionsProvider
    {
        private readonly KeyEncoder keyEncoder;
        private readonly LedgerPath thirdPartyAssetPath = LedgerPath.Parse("/asset/p2pkh/");

        public P2pkhIssuanceImplicitLayout(KeyEncoder keyEncoder)
        {
            this.keyEncoder = keyEncoder;
        }

        public Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, bool recursiveOnly, string recordName)
        {
            HashSet<string> identities = new HashSet<string>(authentication.Select(evidence => keyEncoder.GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);
            LedgerPath pathRecordName;

            // If the path is root and the record name is a tird-party asset owned by the current identity,
            // arbitrary modification of the balance is allowed
            if (LedgerPath.TryParse(recordName, out pathRecordName)
                && thirdPartyAssetPath.IsStrictParentOf(pathRecordName)
                && path.Segments.Count == 0
                && identities.Contains(pathRecordName.Segments[thirdPartyAssetPath.Segments.Count]))
            {
                return Task.FromResult(new PermissionSet(accountNegative: Access.Permit));
            }

            // Account /asset/p2pkh/[addr]/
            if (thirdPartyAssetPath.IsStrictParentOf(path)
                && path.Segments.Count == thirdPartyAssetPath.Segments.Count + 1
                && keyEncoder.IsP2pkh(path.Segments[path.Segments.Count - 1]))
            {
                Access ownAccount = identities.Contains(path.Segments[path.Segments.Count - 1]) && recordName != DynamicPermissionLayout.AclResourceName
                    ? Access.Permit : Access.Unset;

                return Task.FromResult(new PermissionSet(
                    accountModify: Access.Permit,
                    accountSpend: ownAccount,
                    dataModify: ownAccount));
            }
            else
            {
                return Task.FromResult(new PermissionSet());
            }
        }
    }
}
