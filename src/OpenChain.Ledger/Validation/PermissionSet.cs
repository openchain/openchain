using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class PermissionSet
    {
        static PermissionSet()
        {
            AllowAll = new PermissionSet(true, true, true, true, true);
            DenyAll = new PermissionSet(false, false, false, false, false);
        }

        public static PermissionSet AllowAll { get; }

        public static PermissionSet DenyAll { get; }

        public PermissionSet(bool issuance, bool spendFrom, bool affectBalance, bool modifyData, bool modifyPermissions)
        {
            this.Issuance = issuance;
            this.SpendFrom = spendFrom;
            this.AffectBalance = affectBalance;
            this.ModifyData = modifyData;
            this.ModifyPermissions = modifyPermissions;
        }

        public bool Issuance { get; }

        public bool SpendFrom { get; }

        public bool AffectBalance { get; }

        public bool ModifyData { get; }

        public bool ModifyPermissions { get; }

        public PermissionSet Intersect(PermissionSet other)
        {
            return new PermissionSet(
                issuance: Issuance && other.Issuance,
                spendFrom: SpendFrom && other.SpendFrom,
                affectBalance: AffectBalance && other.AffectBalance,
                modifyData: ModifyData && other.ModifyData,
                modifyPermissions: ModifyPermissions && other.ModifyPermissions);
        }
    }
}
