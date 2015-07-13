using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenChain.Ledger
{
    public class ParsedMutation
    {
        private ParsedMutation(IList<AccountStatus> accountMutations, IList<KeyValuePair<LedgerPath, string>> assetDefinitions)
        {
            this.AccountMutations = new ReadOnlyCollection<AccountStatus>(accountMutations);
            this.AssetDefinitions = new ReadOnlyCollection<KeyValuePair<LedgerPath, string>>(assetDefinitions);
        }

        public IReadOnlyList<AccountStatus> AccountMutations { get; }

        public IReadOnlyList<KeyValuePair<LedgerPath, string>> AssetDefinitions { get; }

        public static ParsedMutation Parse(Mutation mutation)
        {
            List<AccountStatus> accountMutations = new List<AccountStatus>();
            List<KeyValuePair<LedgerPath, string>> assetDefinitions = new List<KeyValuePair<LedgerPath, string>>();

            foreach (KeyValuePair pair in mutation.KeyValuePairs)
            {
                BinaryValue key;
                BinaryValue value;

                try
                {
                    key = BinaryValue.Read(pair.Key);
                    value = BinaryValue.Read(pair.Value);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new TransactionInvalidException("InvalidBinaryValue");
                }

                try
                {
                    if (key.Usage == BinaryValueUsage.AccountKey && value.Usage == BinaryValueUsage.Int64)
                        accountMutations.Add(new AccountStatus((AccountKey)key, ((Int64Value)value).Value, pair.Version));
                    else if (key.Usage == BinaryValueUsage.AssetDefinition && value.Usage == BinaryValueUsage.Text)
                        assetDefinitions.Add(new KeyValuePair<LedgerPath, string>(LedgerPath.Parse(((TextValue)key).Value), ((TextValue)value).Value));
                    else
                        throw new TransactionInvalidException("InvalidKeyValuePair");
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "path")
                {
                    throw new TransactionInvalidException("InvalidPath");
                }
            }

            return new ParsedMutation(accountMutations, assetDefinitions);
        }
    }
}
