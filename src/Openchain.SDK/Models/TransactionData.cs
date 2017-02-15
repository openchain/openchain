using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK.Models
{
    public class TransactionData
    {
        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }

        [JsonProperty("mutation")]
        public Mutation Mutation { get; set; }

        [JsonProperty("transaction_hash")]
        public string TransactionHash { get; set; }

        [JsonProperty("mutation_hash")]
        public string MutationHash { get; set; }
    }
}
