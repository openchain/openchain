using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK
{
    public class SigningKey
    {
        [JsonProperty("pub_key")]
        public string PublicKey { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}
