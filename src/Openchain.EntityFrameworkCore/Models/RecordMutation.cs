using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore.Models
{
    public class RecordMutation
    {
        public long TransactionId { get; set; }
        public byte[] RecordKey { get; set; }

        public byte[] MutationHash { get; set; }

        public virtual Transaction Transaction { get; set; }

    }
}
