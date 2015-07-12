using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IRulesValidator
    {
        Task Validate(IReadOnlyList<AccountEntry> accountEntries, IReadOnlyList<AuthenticationEvidence> authentication);
    }
}
