using MongoDB.Bson.Serialization.Attributes;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class AuthenticationEvidence
    {
        public AuthenticationEvidence(string identity, IList<byte[]> evidence)
        {
            this.Identity = identity;
            this.Evidence = evidence;
        }
        
        [BsonElement]
        public string Identity { get; private set; }

        [BsonElement]
        public IList<byte[]> Evidence { get; private set; }
    }
}
