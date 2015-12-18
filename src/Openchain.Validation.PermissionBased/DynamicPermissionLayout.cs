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
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Openchain.Ledger;

namespace Openchain.Validation.PermissionBased
{
    public class DynamicPermissionLayout : IPermissionsProvider
    {
        private readonly IStorageEngine store;
        private readonly KeyEncoder keyEncoder;

        public static string AclResourceName { get; } = "acl";

        public DynamicPermissionLayout(IStorageEngine store, KeyEncoder keyEncoder)
        {
            this.store = store;
            this.keyEncoder = keyEncoder;
        }

        public async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path, bool recursiveOnly, string recordName)
        {
            PermissionSet currentPermissions = PermissionSet.Unset;

            Record record = await this.store.GetRecord(new RecordKey(RecordType.Data, path, AclResourceName));

            if (record.Value.Value.Count == 0)
                return PermissionSet.Unset;

            IReadOnlyList<Acl> permissions;
            try
            {
                permissions = Acl.Parse(Encoding.UTF8.GetString(record.Value.ToByteArray()), path, keyEncoder);
            }
            catch (JsonReaderException)
            {
                return PermissionSet.Unset;
            }
            catch (InvalidOperationException)
            {
                return PermissionSet.Unset;
            }

            foreach (Acl acl in permissions)
            {
                if (acl.IsMatch(identities, path, recursiveOnly, recordName))
                    currentPermissions = currentPermissions.Add(acl.Permissions);
            }

            return currentPermissions;
        }
    }
}
