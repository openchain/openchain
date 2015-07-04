using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpenChain.Server
{
    public class LedgerRecordMetadata
    {
        public LedgerRecordMetadata(int version, IEnumerable<AuthenticationEvidence> authentication)
        {
            this.Version = version;
            this.Authentication = new ReadOnlyCollection<AuthenticationEvidence>(authentication.ToList());
        }

        [BsonElement]
        public int Version { get; private set; }

        [BsonElement]
        public IReadOnlyList<AuthenticationEvidence> Authentication { get; private set; }
    }
}
