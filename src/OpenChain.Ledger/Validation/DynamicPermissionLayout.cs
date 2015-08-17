using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class DynamicPermissionLayout : IPermissionsProvider
    {
        private readonly ITransactionStore store;

        public DynamicPermissionLayout(ITransactionStore store)
        {
            this.store = store;
        }

        public async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path)
        {
            for (int i = 0; i < path.Segments.Count; i++)
            {
                LedgerPath parent = LedgerPath.FromSegments(path.Segments.Take(i).ToArray());

                Record record = await this.store.GetRecord(new RecordKey(RecordType.Data, parent, "acl"));


            }

            return null;
        }
    }
}
