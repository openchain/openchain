using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenChain.Core;

namespace OpenChain.Ledger
{
    public class ParsedMutation
    {
        private ParsedMutation(IList<AccountStatus> accountMutations, IList<KeyValuePair<LedgerPath, string>> assetDefinitions, IList<KeyValuePair<string, LedgerPath>> aliases)
        {
            this.AccountMutations = new ReadOnlyCollection<AccountStatus>(accountMutations);
            this.AssetDefinitions = new ReadOnlyCollection<KeyValuePair<LedgerPath, string>>(assetDefinitions);
            this.Aliases = new ReadOnlyCollection<KeyValuePair<string, LedgerPath>>(aliases);
        }

        public IReadOnlyList<AccountStatus> AccountMutations { get; }

        public IReadOnlyList<KeyValuePair<LedgerPath, string>> AssetDefinitions { get; }

        public IReadOnlyList<KeyValuePair<string, LedgerPath>> Aliases { get; }

        public static ParsedMutation Parse(Mutation mutation)
        {
            List<AccountStatus> accountMutations = new List<AccountStatus>();
            List<KeyValuePair<LedgerPath, string>> assetDefinitions = new List<KeyValuePair<LedgerPath, string>>();
            List<KeyValuePair<string, LedgerPath>> aliases = new List<KeyValuePair<string, LedgerPath>>();

            foreach (KeyValuePair pair in mutation.KeyValuePairs)
            {
                BinaryValue key;
                BinaryValue value;
                
                // This key-value pair does not result in a mutation, it doesn't take part in the validation
                if (pair.Value == null)
                    continue;

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
                    else if (key.Usage == BinaryValueUsage.Alias && value.Usage == BinaryValueUsage.Text)
                        aliases.Add(new KeyValuePair<string, LedgerPath>(((TextValue)key).Value, LedgerPath.Parse(((TextValue)value).Value)));
                    else
                        throw new TransactionInvalidException("InvalidKeyValuePair");
                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "path")
                {
                    throw new TransactionInvalidException("InvalidPath");
                }
            }

            return new ParsedMutation(accountMutations, assetDefinitions, aliases);
        }
    }
}
