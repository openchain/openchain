using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore.Models
{
    public class Transaction
    {
        public long Id { get; set; }
        public byte[] TransactionHash { get; set; }
        public byte[] MutationHash { get; set; }
        public byte[] RawData { get; set; }

        public virtual ICollection<RecordMutation> RecordMutations { get; set; }
    }
}
