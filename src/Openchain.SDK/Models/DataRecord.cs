using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK.Models
{
    public class DataRecord : Record
    {
        public string Data { get; }

        public DataRecord(ByteString key, ByteString value, ByteString version, string data) : base(key, value, version)
        {
            Data = data;
        }
    }
}
