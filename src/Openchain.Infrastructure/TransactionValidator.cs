// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;

namespace Openchain.Infrastructure
{
    public class TransactionValidator
    {
        private static readonly int MaxKeySize = 512;

        private readonly IStorageEngine store;
        private readonly IMutationValidator validator;

        public TransactionValidator(IStorageEngine store, IMutationValidator validator, ByteString @namespace)
        {
            this.store = store;
            this.validator = validator;
            this.Namespace = @namespace;
        }

        public ByteString Namespace { get; }

        public async Task<ByteString> PostTransaction(ByteString rawMutation, IReadOnlyList<SignatureEvidence> authentication)
        {
            Mutation mutation;
            try
            {
                // Verify that the mutation can be deserialized
                mutation = MessageSerializer.DeserializeMutation(rawMutation);
            }
            catch (InvalidProtocolBufferException)
            {
                throw new TransactionInvalidException("InvalidMutation");
            }

            ParsedMutation parsedMutation = ParsedMutation.Parse(mutation);

            IReadOnlyDictionary<AccountKey, AccountStatus> accounts = await ValidateMutation(mutation, parsedMutation);

            ValidateAuthentication(authentication, MessageSerializer.ComputeHash(rawMutation.ToByteArray()));

            DateTime date = DateTime.UtcNow;

            IList<Mutation> generatedMutations = await this.validator.Validate(parsedMutation, authentication, accounts);

            TransactionMetadata metadata = new TransactionMetadata(authentication);

            byte[] rawMetadata = SerializeMetadata(metadata);

            Transaction transaction = new Transaction(rawMutation, date, new ByteString(rawMetadata));
            byte[] serializedTransaction = MessageSerializer.SerializeTransaction(transaction);

            List<ByteString> transactions = new List<ByteString>() { new ByteString(serializedTransaction) };

            transactions.AddRange(await Task.WhenAll(generatedMutations.Select(async generatedMutation =>
            {
                await ValidateMutation(generatedMutation, ParsedMutation.Parse(generatedMutation));
                Transaction generatedTransaction = new Transaction(new ByteString(MessageSerializer.SerializeMutation(generatedMutation)), date, ByteString.Empty);
                return new ByteString(MessageSerializer.SerializeTransaction(generatedTransaction));
            })));

            try
            {
                await this.store.AddTransactions(transactions);
            }
            catch (ConcurrentMutationException)
            {
                throw new TransactionInvalidException("OptimisticConcurrency");
            }

            return new ByteString(MessageSerializer.ComputeHash(serializedTransaction));
        }

        private async Task<IReadOnlyDictionary<AccountKey, AccountStatus>> ValidateMutation(Mutation mutation, ParsedMutation parsedMutation)
        {
            if (!mutation.Namespace.Equals(this.Namespace))
                throw new TransactionInvalidException("InvalidNamespace");

            if (mutation.Records.Count == 0)
                throw new TransactionInvalidException("InvalidMutation");

            if (mutation.Records.Any(record => record.Key.Value.Count > MaxKeySize))
                throw new TransactionInvalidException("InvalidMutation");

            // All assets must have an overall zero balance

            IReadOnlyDictionary<AccountKey, AccountStatus> accounts =
                await this.store.GetAccounts(parsedMutation.AccountMutations.Select(entry => entry.AccountKey));

            var groups = parsedMutation.AccountMutations
                .GroupBy(account => account.AccountKey.Asset.FullPath)
                .Select(group => group.Sum(entry => entry.Balance - accounts[entry.AccountKey].Balance));

            if (groups.Any(group => group != 0))
                throw new TransactionInvalidException("UnbalancedTransaction");

            return accounts;
        }

        private static void ValidateAuthentication(IReadOnlyList<SignatureEvidence> authentication, byte[] mutationHash)
        {
            foreach (SignatureEvidence evidence in authentication)
            {
                if (!evidence.VerifySignature(mutationHash))
                    throw new TransactionInvalidException("InvalidSignature");
            }
        }

        private byte[] SerializeMetadata(TransactionMetadata metadata)
        {
            Messages.TransactionMetadata transactionMetadataBuilder = new Messages.TransactionMetadata();
            transactionMetadataBuilder.Signatures.Add(metadata.Signatures.Select(
                signature => new Messages.TransactionMetadata.Types.SignatureEvidence()
                {
                    PublicKey = Google.Protobuf.ByteString.CopyFrom(signature.PublicKey.ToByteArray()),
                    Signature = Google.Protobuf.ByteString.CopyFrom(signature.Signature.ToByteArray())
                }));

            return transactionMetadataBuilder.ToByteArray();
        }
    }
}
