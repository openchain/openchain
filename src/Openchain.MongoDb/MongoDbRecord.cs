using Openchain.Infrastructure;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Openchain.MongoDb
{
    public class MongoDbRecord
    {
        [BsonId]
        public byte[] Key
        {
            get;
            set;
        }

        public string KeyS
        {
            get;
            set;
        }

        public byte[] Value
        {
            get;
            set;
        }

        public byte[] Version
        {
            get;
            set;
        }

        public string[] Path
        {
            get;
            set;
        }

        public RecordType Type
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public byte[] TransactionLock { get; set; }

        [BsonExtraElements]
        public BsonDocument Extra { get; set; }

    }
}