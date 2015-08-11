using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IAnchorBuilder
    {
        Task<LedgerAnchor> CreateAnchor();
    }
}
