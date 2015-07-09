using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public class NullValidator : IRulesValidator
    {
        private readonly bool alwaysValid;

        public NullValidator(bool alwaysValid)
        {
            this.alwaysValid = alwaysValid;
        }

        public Task Validate(Transaction transaction, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            if (this.alwaysValid)
                return Task.FromResult(0);
            else
                throw new TransactionInvalidException("Disabled");
        }
    }
}
