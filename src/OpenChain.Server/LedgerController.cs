using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public class LedgerController
    {
        private readonly ITransactionValidator validator;

        public LedgerController(ILedgerStore store, ITransactionValidator validator)
        {
            this.Store = store;
            this.validator = validator;
        }

        public ILedgerStore Store { get; }

        public async Task<BinaryData> PostTransaction(BinaryData rawTransaction, IEnumerable<AuthenticationEvidence> authentication)
        {
            Transaction transaction = TransactionSerializer.DeserializeTransaction(rawTransaction.ToArray());

            // All assets must have an overall zero balance
            var groups = transaction.AccountEntries
                .GroupBy(entry => entry.AccountKey.Asset)
                .Select(group => group.Sum(entry => entry.Amount));

            if (groups.Any(group => group != 0))
                return null;

            DateTime date = DateTime.UtcNow;

            LedgerRecordMetadata recordMetadata = new LedgerRecordMetadata(1, authentication);

            if (!await this.validator.IsValid(transaction, recordMetadata.Authentication))
                return null;
            
            byte[] metadata = MongoDB.Bson.BsonExtensionMethods.ToBson<LedgerRecordMetadata>(recordMetadata);

            return await this.Store.AddTransaction(rawTransaction, date, new BinaryData(metadata));
        }

        public async Task<BinaryData> AddRecord(BinaryData rawLedgerRecord)
        {
            return await this.Store.AddLedgerRecord(rawLedgerRecord);
        }
    }
}
