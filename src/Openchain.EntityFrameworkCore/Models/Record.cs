using Openchain.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore.Models
{
    public class Record
    {
        public byte[] Key { get; set; }
        public byte[] Value { get; set; }
        public byte[] Version { get; set; }
        public string Name { get; set; }
        public RecordType Type { get; set; }
    }
}
