using System.Collections.Generic;
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

        public async Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path, bool recursiveOnly, string recordName)
        {
            PermissionSet currentPermissions = PermissionSet.DenyAll;

            Record record = await this.store.GetRecord(new RecordKey(RecordType.Data, path, "acl"));

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
