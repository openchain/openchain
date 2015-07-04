using MongoDB.Bson.Serialization.Attributes;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public class AuthenticationEvidence
    {
        public AuthenticationEvidence(string account, string asset, string identity, byte[] evidence)
        {
            this.Account = account;
            this.Asset = asset;
            this.Identity = identity;
            this.Evidence = evidence;
        }

        [BsonElement]
        public string Account { get; private set; }

        [BsonElement]
        public string Asset { get; private set; }

        [BsonElement]
        public string Identity { get; private set; }

        [BsonElement]
        public byte[] Evidence { get; private set; }
    }
}
