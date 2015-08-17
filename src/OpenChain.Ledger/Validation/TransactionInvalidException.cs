using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger.Validation
{
    public class TransactionInvalidException : Exception
    {
        public TransactionInvalidException(string reason)
            : base(string.Format("The transaction was rejected: {0}.", reason))
        { }
    }
}
