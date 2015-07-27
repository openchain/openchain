using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class PermissionSet
    {
        public PermissionSet(bool issuance, bool spendFrom, bool affectBalance, bool modifyAlias)
        {
            this.Issuance = issuance;
            this.SpendFrom = spendFrom;
            this.AffectBalance = affectBalance;
            this.ModifyAlias = modifyAlias;
        }

        public bool Issuance { get; }

        public bool SpendFrom { get; }

        public bool AffectBalance { get; }

        public bool ModifyAlias { get; }
    }
}
