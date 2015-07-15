using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IRulesValidator
    {
        Task ValidateAccountMutations(IReadOnlyList<AccountStatus> accountMutations, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts);

        Task ValidateAssetDefinitionMutations(IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations, IReadOnlyList<SignatureEvidence> authentication);
    }
}
