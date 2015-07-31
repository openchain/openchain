using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class PermissionSet
    {
        public PermissionSet(bool issuance, bool spendFrom, bool affectBalance, bool modifyData)
        {
            this.Issuance = issuance;
            this.SpendFrom = spendFrom;
            this.AffectBalance = affectBalance;
            this.ModifyData = modifyData;
        }

        public bool Issuance { get; }

        public bool SpendFrom { get; }

        public bool AffectBalance { get; }

        public bool ModifyData { get; }
    }
}
