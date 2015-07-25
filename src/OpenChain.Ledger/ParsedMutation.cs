using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

            foreach (Record record in mutation.Records)
            {
                BinaryValue key;
                BinaryValue value;
                
                // This record does not result in a mutation, it doesn't take part in the validation
                if (record.Value == null)
                    continue;

                try
                {
                    key = BinaryValue.Read(record.Key, isKey: true);
                    value = BinaryValue.Read(record.Value, isKey: false);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new TransactionInvalidException("InvalidBinaryValue");
                }

                try
                {
                    if (key.Usage == BinaryValueUsage.Account && key.Type == BinaryValueType.StringPair && value.Type == BinaryValueType.Int64)
                        accountMutations.Add(new AccountStatus((AccountKey)key, ((Int64Value)value).Value, record.Version));
                    else if (key.Usage == BinaryValueUsage.AssetDefinition && key.Type == BinaryValueType.String && value.Type == BinaryValueType.String)
                        assetDefinitions.Add(new KeyValuePair<LedgerPath, string>(LedgerPath.Parse(((TextValue)key).Value), ((TextValue)value).Value));
                    else if (key.Usage == BinaryValueUsage.Alias && key.Type == BinaryValueType.String && value.Type == BinaryValueType.String)
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
