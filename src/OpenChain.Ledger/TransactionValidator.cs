using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    public class TransactionValidator
    {
        private readonly ITransactionStore store;
        private readonly IRulesValidator validator;
        private readonly BinaryData ledgerId;

        public TransactionValidator(ITransactionStore store, IRulesValidator validator, BinaryData ledgerId)
        {
            this.store = store;
            this.validator = validator;
            this.ledgerId = ledgerId;
        }

        public async Task<BinaryData> PostTransaction(BinaryData rawMutation, IReadOnlyList<AuthenticationEvidence> authentication)
        {
            // Verify that the mutation set can be deserialized
            Mutation mutation = MessageSerializer.DeserializeMutation(rawMutation);

            if (!mutation.Namespace.Equals(this.ledgerId))
                throw new TransactionInvalidException("InvalidNamespace");

            IReadOnlyList<AccountStatus> accountEntries = mutation.KeyValuePairs.Select(AccountStatus.FromKeyValuePair).ToList();

            if (accountEntries.Any(item => item == null))
                throw new TransactionInvalidException("NotAccountMutation");

            // All assets must have an overall zero balance
            var groups = accountEntries
                .GroupBy(entry => entry.AccountKey.Asset)
                .Select(group => group.Sum(entry => entry.Balance));

            if (groups.Any(group => group != 0))
                throw new TransactionInvalidException("UnbalancedTransaction");

            // There must not be the same account represented twice
            var groupedAccountEntries = accountEntries
                .GroupBy(entry => entry.AccountKey, entry => entry);

            if (groupedAccountEntries.Any(group => group.Count() > 1))
                throw new TransactionInvalidException("DuplicateAccount");

            // Paths must be correctly formatted
            if (!accountEntries.All(
                account => LedgerPath.IsValidPath(account.AccountKey.Account) && LedgerPath.IsValidPath(account.AccountKey.Asset)))
                throw new TransactionInvalidException("InvalidPath");

            DateTime date = DateTime.UtcNow;
            
            await this.validator.Validate(accountEntries, authentication);

            LedgerRecordMetadata recordMetadata = new LedgerRecordMetadata(1, authentication);

            byte[] metadata = BsonExtensionMethods.ToBson<LedgerRecordMetadata>(recordMetadata);

            Transaction transaction = new Transaction(rawMutation, date, new BinaryData(metadata));
            byte[] serializedTransaction = MessageSerializer.SerializeTransaction(transaction);

            try
            {
                await this.store.AddTransactions(new[] { new BinaryData(serializedTransaction) });
            }
            catch (ConcurrentMutationException)
            {
                throw new TransactionInvalidException("OptimisticConcurrency");
            }

            return new BinaryData(MessageSerializer.ComputeHash(serializedTransaction));
        }
    }
}
