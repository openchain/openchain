using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public interface IAnchorRecorder
    {
        Task RecordAnchor(LedgerAnchor anchor);
    }
}
