using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class PermissionSet
    {
        public static PermissionSet AllowAll { get; } = new PermissionSet(Access.Permit, Access.Permit, Access.Permit, Access.Permit);

        public static PermissionSet DenyAll { get; } = new PermissionSet(Access.Deny, Access.Deny, Access.Deny, Access.Deny);

        public static PermissionSet Unset { get; } = new PermissionSet(Access.Unset, Access.Unset, Access.Unset, Access.Unset);

        public PermissionSet(
            Access accountNegative = Access.Unset,
            Access accountSpend = Access.Unset,
            Access accountModify = Access.Unset,
            Access dataModify = Access.Unset)
        {
            this.AccountNegative = accountNegative;
            this.AccountSpend = accountSpend;
            this.AccountModify = accountModify;
            this.DataModify = dataModify;
        }

        public Access AccountNegative { get; }

        public Access AccountSpend { get; }

        public Access AccountModify { get; }

        public Access DataModify { get; }

        public PermissionSet Add(PermissionSet added)
        {
            return new PermissionSet(
                accountNegative: Or(AccountNegative, added.AccountNegative),
                accountSpend: Or(AccountSpend, added.AccountSpend),
                accountModify: Or(AccountModify, added.AccountModify),
                dataModify: Or(DataModify, added.DataModify));
        }

        private static Access Or(Access left, Access right)
        {
            if (left == Access.Deny || right == Access.Deny)
                return Access.Deny;
            else if (left == Access.Permit || right == Access.Permit)
                return Access.Permit;
            else
                return Access.Unset;
        }

        public PermissionSet AddLevel(PermissionSet lowerLevel)
        {
            return new PermissionSet(
                accountNegative: lowerLevel.AccountNegative == Access.Unset ? AccountNegative : lowerLevel.AccountNegative,
                accountSpend: lowerLevel.AccountSpend == Access.Unset ? AccountSpend : lowerLevel.AccountSpend,
                accountModify: lowerLevel.AccountModify == Access.Unset ? AccountModify : lowerLevel.AccountModify,
                dataModify: lowerLevel.DataModify == Access.Unset ? DataModify : lowerLevel.DataModify);
        }
    }
}
