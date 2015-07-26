using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenChain.Ledger
{
    public class ParsedMutation
    {
        private ParsedMutation(IList<AccountStatus> accountMutations, IList<KeyValuePair<LedgerPath, string>> assetDefinitions, IList<KeyValuePair<LedgerPath, LedgerPath>> aliases)
        {
            this.AccountMutations = new ReadOnlyCollection<AccountStatus>(accountMutations);
            this.AssetDefinitions = new ReadOnlyCollection<KeyValuePair<LedgerPath, string>>(assetDefinitions);
            this.Aliases = new ReadOnlyCollection<KeyValuePair<LedgerPath, LedgerPath>>(aliases);
        }

        public IReadOnlyList<AccountStatus> AccountMutations { get; }

        public IReadOnlyList<KeyValuePair<LedgerPath, string>> AssetDefinitions { get; }

        public IReadOnlyList<KeyValuePair<LedgerPath, LedgerPath>> Aliases { get; }

        public static ParsedMutation Parse(Mutation mutation)
        {
            List<AccountStatus> accountMutations = new List<AccountStatus>();
            List<KeyValuePair<LedgerPath, string>> assetDefinitions = new List<KeyValuePair<LedgerPath, string>>();
            List<KeyValuePair<LedgerPath, LedgerPath>> aliases = new List<KeyValuePair<LedgerPath, LedgerPath>>();

            foreach (Record record in mutation.Records)
            {
                // This is used for optimistic concurrency and does not participate in the validation
                if (record.Value == null)
                    continue;

                try
                {
                    RecordKey key = RecordKey.Parse(record.Key);
                    switch (key.RecordType)
                    {
                        case RecordType.Account:
                            accountMutations.Add(AccountStatus.FromRecord(key, record));
                            break;
                        case RecordType.AssetDefinition:
                            assetDefinitions.Add(new KeyValuePair<LedgerPath, string>(
                                key.Path,
                                Encoding.UTF8.GetString(record.Value.ToByteArray())));
                            break;
                        case RecordType.Alias:
                            aliases.Add(new KeyValuePair<LedgerPath, LedgerPath>(
                                key.Path,
                                LedgerPath.Parse(Encoding.UTF8.GetString(record.Value.ToByteArray()))));
                            break;
                        default:
                            throw new TransactionInvalidException("InvalidRecord");
                    }

                }
                catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "keyData")
                {
                    throw new TransactionInvalidException("NonCanonicalSerialization");
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new TransactionInvalidException("InvalidPath");
                }
            }

            return new ParsedMutation(accountMutations, assetDefinitions, aliases);
        }
    }
}
