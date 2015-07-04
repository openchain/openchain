using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public interface ITransactionValidator
    {
        Task<bool> IsValid(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication);
    }
}
