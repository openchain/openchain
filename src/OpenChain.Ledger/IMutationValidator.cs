using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IMutationValidator
    {
        Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts);
    }
}
