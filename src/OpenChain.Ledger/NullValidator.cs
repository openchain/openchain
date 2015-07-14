using OpenChain.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace OpenChain.Ledger
{
    public class NullValidator : IRulesValidator
    {
        private readonly bool alwaysValid;

        public NullValidator(bool alwaysValid)
        {
            this.alwaysValid = alwaysValid;
        }

        public Task ValidateAccountMutations(IReadOnlyList<AccountStatus> accountMutations, IReadOnlyList<SignatureEvidence> authentication)
        {
            if (this.alwaysValid)
                return Task.FromResult(0);
            else
                throw new TransactionInvalidException("Disabled");
        }

        public Task ValidateAssetDefinitionMutations(IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations, IReadOnlyList<SignatureEvidence> authentication)
        {
            if (this.alwaysValid)
                return Task.FromResult(0);
            else
                throw new TransactionInvalidException("Disabled");
        }
    }
}
