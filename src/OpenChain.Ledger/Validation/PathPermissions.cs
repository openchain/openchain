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
using System.Linq;

namespace OpenChain.Ledger.Validation
{
    public class PathPermissions
    {
        public PathPermissions(LedgerPath path, PermissionSet permissions, IEnumerable<string> identities)
        {
            this.Path = path;
            this.Permissions = permissions;
            this.Identities = identities.ToList().AsReadOnly();
        }

        public LedgerPath Path { get; }

        public PermissionSet Permissions { get; }

        public IReadOnlyList<string> Identities { get; }
    }
}
