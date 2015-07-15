using Google.ProtocolBuffers;
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

        public async Task<BinaryData> PostTransaction(BinaryData rawMutation, IReadOnlyList<SignatureEvidence> authentication)
        {
            // Verify that the mutation set can be deserialized
            Mutation mutation = MessageSerializer.DeserializeMutation(rawMutation);

            if (!mutation.Namespace.Equals(this.ledgerId))
                throw new TransactionInvalidException("InvalidNamespace");

            // There must not be the same key represented twice
            var groupedPairs = mutation.KeyValuePairs
                .GroupBy(pair => pair.Key, pair => pair);

            if (groupedPairs.Any(group => group.Count() > 1))
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

            await this.validator.ValidateAccountMutations(parsedMutation.AccountMutations, authentication, accounts);
            await this.validator.ValidateAssetDefinitionMutations(parsedMutation.AssetDefinitions, authentication);

            TransactionMetadata metadata = new TransactionMetadata(authentication);

            byte[] rawMetadata = SerializeMetadata(metadata);

            Transaction transaction = new Transaction(rawMutation, date, new BinaryData(rawMetadata));
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
            Messages.TransactionMetadata.Builder transactionMetadataBuilder = new Messages.TransactionMetadata.Builder();
            transactionMetadataBuilder.AddRangeSignatures(metadata.Signatures.Select(
                signature => new Messages.TransactionMetadata.Types.SignatureEvidence.Builder()
                {
                    PublicKey = ByteString.Unsafe.FromBytes(signature.PublicKey.ToByteArray()),
                    Signature = ByteString.Unsafe.FromBytes(signature.Signature.ToByteArray())
                }
                .Build()));

            return transactionMetadataBuilder.BuildParsed().ToByteArray();
        }
    }
}
