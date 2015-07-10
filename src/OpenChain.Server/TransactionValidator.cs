using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Server
{
    public class TransactionValidator
    {
        private readonly ILedgerStore store;
        private readonly IRulesValidator validator;

        public TransactionValidator(ILedgerStore store, IRulesValidator validator)
        {
            this.store = store;
            this.validator = validator;
        }

        public async Task<BinaryData> PostTransaction(BinaryData rawTransaction, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            // Verify that the transaction can be deserialized
            Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction.ToByteArray());

            // All assets must have an overall zero balance
            var groups = transaction.AccountEntries
                .GroupBy(entry => entry.AccountKey.Asset)
                .Select(group => group.Sum(entry => entry.Amount));

            if (groups.Any(group => group != 0))
                return null;

            // There must not be the same account represented twice
            var accountEntries = transaction.AccountEntries
                .GroupBy(entry => entry.AccountKey, entry => entry);

            if (accountEntries.Any(group => group.Count() > 1))
                return null;

            // Paths must be correctly formatted
            if (!transaction.AccountEntries.All(
                account => LedgerPath.IsValidPath(account.AccountKey.Account) && LedgerPath.IsValidPath(account.AccountKey.Asset)))
                return null;

            DateTime date = DateTime.UtcNow;
            
            await this.validator.Validate(transaction, authentication);

            LedgerRecordMetadata recordMetadata = new LedgerRecordMetadata(1, authentication);

            byte[] metadata = BsonExtensionMethods.ToBson<LedgerRecordMetadata>(recordMetadata);

            LedgerRecord record = new LedgerRecord(rawTransaction, date, new BinaryData(metadata));
            byte[] serializedLedgerRecord = MessageSerializer.SerializeLedgerRecord(record);
            await this.store.AddLedgerRecords(new[] { new BinaryData(serializedLedgerRecord) });

            return new BinaryData(MessageSerializer.ComputeHash(serializedLedgerRecord));
        }
    }
}
