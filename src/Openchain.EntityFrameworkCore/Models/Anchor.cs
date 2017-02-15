using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore.Models
{
    public class Anchor
    {
        public int Id { get; set; }
        public byte[] Position { get; set; }
        public byte[] FullLedgerHash { get; set; }
        public long TransactionCount { get; set; }
        public byte[] AnchorId { get; set; }
    }
}
