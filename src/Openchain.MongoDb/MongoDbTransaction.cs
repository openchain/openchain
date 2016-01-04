using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;
using System;

namespace Openchain.MongoDb
{
    public class MongoDbTransaction
    {
        [BsonId]
        public byte[] TransactionHash
        {
            get;
            set;
        }

        public byte[] MutationHash
        {
            get;
            set;
        }

        public byte[] RawData
        {
            get;
            set;
        }

        public List<byte[]> Records
        {
            get;
            set;
        }

        public BsonTimestamp Timestamp
        {
            get;
            set;
        } = new BsonTimestamp(0);

    }

    public class MongoDbPendingTransaction
    {
        [BsonId]
        public byte[] TransactionHash
        {
            get;
            set;
        }

        public byte[] MutationHash
        {
            get;
            set;
        }

        public byte[] RawData
        {
            get;
            set;
        }

        public List<MongoDbRecord> InitialRecords
        {
            get;
            set;
        }

        public BsonTimestamp Timestamp
        {
            get;
            set;
        } = new BsonTimestamp(0);

        public DateTime LockTimestamp
        {
            get;
            set;
        }

        [BsonExtraElements]
        public BsonDocument Extra { get; set; }

    }

}