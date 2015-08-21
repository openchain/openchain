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

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class DynamicPermissionLayout : IPermissionsProvider
    {
        private readonly ITransactionStore store;
        private readonly KeyEncoder keyEncoder;

        public static string AclResourceName { get; } = "acl";

        public DynamicPermissionLayout(ITransactionStore store, KeyEncoder keyEncoder)
        {
            this.store = store;
            this.keyEncoder = keyEncoder;
        }

        public async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path, bool recursiveOnly, string recordName)
        {
            PermissionSet currentPermissions = PermissionSet.DenyAll;

            Record record = await this.store.GetRecord(new RecordKey(RecordType.Data, path, AclResourceName));

            IReadOnlyList<Acl> permissions = Acl.Parse(Encoding.UTF8.GetString(record.Value.ToByteArray()), keyEncoder);

            foreach (Acl acl in permissions)
            {
                if (acl.IsMatch(identities, path, recursiveOnly, recordName))
                    currentPermissions = currentPermissions.Add(acl.Permissions);
            }

            return currentPermissions;
        }
    }
}
