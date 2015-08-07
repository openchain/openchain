using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface ILedgerQueries
    {
        Task<IReadOnlyList<Record>> GetKeyStartingFrom(ByteString prefix);

        Task<ByteString> GetTransaction(ByteString mutationHash);
    }
}
