using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public interface IRulesValidator
    {
        Task Validate(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication);
    }
}
