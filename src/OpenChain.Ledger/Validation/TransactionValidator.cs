using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace OpenChain.Ledger.Validation
{
    public class TransactionValidator
    {
        private readonly ITransactionStore store;
        private readonly ByteString ledgerId;
        private readonly IMutationValidator validator;

        public TransactionValidator(ITransactionStore store, IMutationValidator validator, string rootUrl)
        {
            this.store = store;
            this.validator = validator;
            this.RootUrl = rootUrl;
            this.ledgerId = new ByteString(Encoding.UTF8.GetBytes(rootUrl));
        }

        public string RootUrl { get; }

        public async Task<ByteString> PostTransaction(ByteString rawMutation, IReadOnlyList<SignatureEvidence> authentication)
        {
            // Verify that the mutation set can be deserialized
            Mutation mutation = MessageSerializer.DeserializeMutation(rawMutation);

            if (!mutation.Namespace.Equals(this.ledgerId))
                throw new TransactionInvalidException("InvalidNamespace");

            // There must not be the same key represented twice
            var groupedRecords = mutation.Records
                .GroupBy(record => record.Key, record => record);

            if (groupedRecords.Any(group => group.Count() > 1))
                throw new TransactionInvalidException("DuplicateKey");

            ValidateAuthentication(authentication, MessageSerializer.ComputeHash(rawMutation.ToByteArray()));

            ParsedMutation parsedMutation = ParsedMutation.Parse(mutation);

            // All assets must have an overall zero balance

            IReadOnlyDictionary<AccountKey, AccountStatus> accounts =
                await this.store.GetAccounts(parsedMutation.AccountMutations.Select(entry => entry.AccountKey));

            var groups = parsedMutation.AccountMutations
                .GroupBy(account => account.AccountKey.Asset.FullPath)
                .Select(group => group.Sum(entry => entry.Balance - accounts[entry.AccountKey].Balance));

            if (groups.Any(group => group != 0))
                throw new TransactionInvalidException("UnbalancedTransaction");

            DateTime date = DateTime.UtcNow;

            await this.validator.Validate(parsedMutation, authentication, accounts);

            TransactionMetadata metadata = new TransactionMetadata(authentication);

            byte[] rawMetadata = SerializeMetadata(metadata);

            Transaction transaction = new Transaction(rawMutation, date, new ByteString(rawMetadata));
            byte[] serializedTransaction = MessageSerializer.SerializeTransaction(transaction);

            try
            {
                await this.store.AddTransactions(new[] { new ByteString(serializedTransaction) });
            }
            catch (ConcurrentMutationException)
            {
                throw new TransactionInvalidException("OptimisticConcurrency");
            }

            return new ByteString(MessageSerializer.ComputeHash(serializedTransaction));
        }

        private static void ValidateAuthentication(IReadOnlyList<SignatureEvidence> authentication, byte[] mutationHash)
        {
            foreach (SignatureEvidence evidence in authentication)
            {
                ECKey key = new ECKey(evidence.PublicKey.ToByteArray());

                if (!key.VerifySignature(mutationHash, evidence.Signature.ToByteArray()))
                    throw new TransactionInvalidException("InvalidSignature");
            }
        }

        private byte[] SerializeMetadata(TransactionMetadata metadata)
        {
            Messages.TransactionMetadata transactionMetadataBuilder = new Messages.TransactionMetadata();
            transactionMetadataBuilder.Signatures.Add(metadata.Signatures.Select(
                signature => new Messages.TransactionMetadata.Types.SignatureEvidence()
                {
                    PublicKey = Google.Protobuf.ByteString.Unsafe.FromBytes(signature.PublicKey.ToByteArray()),
                    Signature = Google.Protobuf.ByteString.Unsafe.FromBytes(signature.Signature.ToByteArray())
                }));

            return transactionMetadataBuilder.ToByteArray();
        }
    }
}
