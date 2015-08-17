using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class NullValidator : IMutationValidator
    {
        private readonly bool alwaysValid;

        public NullValidator(bool alwaysValid)
        {
            this.alwaysValid = alwaysValid;
        }

        public Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
        {
            if (this.alwaysValid)
                return Task.FromResult(0);
            else
                throw new TransactionInvalidException("Disabled");
        }
    }
}
