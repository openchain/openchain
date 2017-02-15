using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK.Models
{
    public class LedgerInfo
    {
        [JsonProperty("namespace")]
        public string Namespace { get; set; }
    }
}
