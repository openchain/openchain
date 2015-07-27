using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class DefaultPermissionLayout : IPermissionsProvider
    {
        private readonly HashSet<string> adminAddresses;
        private readonly bool allowThirdPartyAssets;
        private readonly LedgerPath assetPath = LedgerPath.Parse("/asset");
        private readonly LedgerPath thirdPartyAssetPath = LedgerPath.Parse("/asset/p2pkh");
        private readonly LedgerPath p2pkhAccountPath = LedgerPath.Parse("/p2pkh");
        private readonly LedgerPath aliasPath = LedgerPath.Parse("/aka");

        public DefaultPermissionLayout(string[] adminAddresses, bool allowThirdPartyAssets)
        {
            this.adminAddresses = new HashSet<string>(adminAddresses, StringComparer.Ordinal);
            this.allowThirdPartyAssets = allowThirdPartyAssets;
        }

        public Task<PermissionSet> GetPermissions(IReadOnlyList<string> identities, LedgerPath path)
        {
            bool isAliasPath = aliasPath.IsStrictParentOf(path);
            bool isAccountPath = p2pkhAccountPath.IsStrictParentOf(path);
            bool isAssetPath = assetPath.IsStrictParentOf(path);

            if (identities.Any(identity => this.adminAddresses.Contains(identity)))
            {
                return Task.FromResult(new PermissionSet(issuance: isAssetPath, spendFrom: true, affectBalance: isAccountPath, modifyAlias: isAliasPath));
            }
            else
            {
                bool issuancePath = this.allowThirdPartyAssets
                        && thirdPartyAssetPath.IsStrictParentOf(path)
                        && identities.Contains(path.Segments[2]);

                return Task.FromResult(new PermissionSet(
                    issuance: issuancePath,
                    spendFrom: isAccountPath && identities.Contains(path.Segments[1]),
                    affectBalance: (isAccountPath && IsP2pkh(path.Segments[1])) || issuancePath,
                    modifyAlias: false));
            }
        }

        private static bool IsP2pkh(string address)
        {
            try
            {
                byte[] pubKeyHash = Base58CheckEncoding.Decode(address);
                if (pubKeyHash.Length != 21 || pubKeyHash[0] != 0)
                    return false;
                else
                    return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
