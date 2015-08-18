using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class DefaultPermissionLayout : IPermissionsProvider
    {
        private readonly bool allowThirdPartyAssets;
        private readonly KeyEncoder keyEncoder;
        private readonly LedgerPath assetPath = LedgerPath.Parse("/asset/");
        private readonly LedgerPath thirdPartyAssetPath = LedgerPath.Parse("/asset/p2pkh/");
        private readonly LedgerPath p2pkhAccountPath = LedgerPath.Parse("/p2pkh/");

        public DefaultPermissionLayout(bool allowThirdPartyAssets, KeyEncoder keyEncoder)
        {
            this.allowThirdPartyAssets = allowThirdPartyAssets;
            this.keyEncoder = keyEncoder;
        }

        public Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, string recordName)
        {
            IReadOnlyList<string> identities = authentication.Select(evidence => keyEncoder.GetPubKeyHash(evidence.PublicKey)).ToList().AsReadOnly();

            // Is the record path under /p2pkh/
            bool isAccountPath = p2pkhAccountPath.IsStrictParentOf(path);
            // Is the record path under /asset/
            bool isAssetPath = assetPath.IsStrictParentOf(path);

            // If the account is /p2pkh/<signed-key>, you can spend from that account, and modify DATA records
            bool canSpend = isAccountPath && identities.Contains(path.Segments[1]);
            // If the account is under /p2pkh/<valid-key> or under /asset/, you can send to that account
            bool validPath = (isAccountPath && keyEncoder.IsP2pkh(path.Segments[1])) || isAssetPath;

            // If the record name is under /asset/p2pkh/<signed-key>, you can issue from that account
            bool isIssuer = false;
            LedgerPath pathRecordName;
            if (LedgerPath.TryParse(recordName, out pathRecordName))
            {
                isIssuer = this.allowThirdPartyAssets
                    && thirdPartyAssetPath.IsStrictParentOf(pathRecordName)
                    && identities.Contains(pathRecordName.Segments[2]);
            }

            // If the path is under /asset/p2pkh/<signed-key>, you can modify DATA records
            bool isOwnAssetPath = this.allowThirdPartyAssets
                && thirdPartyAssetPath.IsStrictParentOf(path)
                && identities.Contains(path.Segments[2]);

            return Task.FromResult(new PermissionSet(
                accountNegative: isIssuer,
                accountSpend: canSpend,
                accountModify: validPath,
                dataModify: isOwnAssetPath || canSpend));
        }
    }
}
