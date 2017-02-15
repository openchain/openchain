using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK.Models
{
    public class AclSubject
    {
        public AclSubject()
        {
            Addresses = new List<string>();
            Required = 0;
        }

        [JsonProperty("addresses")]
        public List<string> Addresses { get; set; }

        [JsonProperty("required")]
        public int Required { get; set; }
    }
}
