using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IRulesValidator
    {
        Task ValidateAccountMutations(IReadOnlyList<AccountStatus> accountMutations, IReadOnlyList<AuthenticationEvidence> authentication);

        Task ValidateAssetDefinitionMutations(IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations, IReadOnlyList<AuthenticationEvidence> authentication);
    }
}
