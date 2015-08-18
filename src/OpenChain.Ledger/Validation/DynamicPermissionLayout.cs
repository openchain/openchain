using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class DynamicPermissionLayout : IPermissionsProvider
    {
        private readonly ITransactionStore store;
        private readonly KeyEncoder keyEncoder;

        public DynamicPermissionLayout(ITransactionStore store, KeyEncoder keyEncoder)
        {
            this.store = store;
            this.keyEncoder = keyEncoder;
        }

        public async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path, string recordName)
        {
            PermissionSet currentPermissions = PermissionSet.DenyAll;

            for (int i = 0; i < path.Segments.Count; i++)
            {
                LedgerPath parent = LedgerPath.FromSegments(path.Segments.Take(i).ToArray());

                Record record = await this.store.GetRecord(new RecordKey(RecordType.Data, parent, "acl"));

                Acl acl = Acl.Parse(Encoding.UTF8.GetString(record.Value.ToByteArray()), keyEncoder);

                if (acl.IsMatch(identities, path, recordName))
                    currentPermissions = currentPermissions.Add(acl.Permissions);
            }

            return currentPermissions;
        }
    }
}
