using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class PermissionSet
    {
        public static PermissionSet AllowAll { get; } = new PermissionSet(true, true, true, true);

        public static PermissionSet DenyAll { get; } = new PermissionSet(false, false, false, false);

        public PermissionSet(bool accountNegative, bool accountSpend, bool accountModify, bool dataModify)
        {
            this.AccountNegative = accountNegative;
            this.AccountSpend = accountSpend;
            this.AccountModify = accountModify;
            this.DataModify = dataModify;
        }

        public bool AccountNegative { get; }

        public bool AccountSpend { get; }

        public bool AccountModify { get; }

        public bool DataModify { get; }

        public PermissionSet Add(PermissionSet added)
        {
            return new PermissionSet(
                accountNegative: AccountNegative || added.AccountNegative,
                accountSpend: AccountSpend || added.AccountSpend,
                accountModify: AccountModify || added.AccountModify,
                dataModify: DataModify || added.DataModify);
        }

        public PermissionSet Intersect(PermissionSet other)
        {
            return new PermissionSet(
                accountNegative: AccountNegative && other.AccountNegative,
                accountSpend: AccountSpend && other.AccountSpend,
                accountModify: AccountModify && other.AccountModify,
                dataModify: DataModify && other.DataModify);
        }
    }
}
