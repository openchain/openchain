using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace OpenChain.Ledger
{
    public class ParsedMutation
    {
        public ParsedMutation(
            IList<AccountStatus> accountMutations,
            IList<KeyValuePair<LedgerPath, BinaryData>> dataRecords)
        {
            this.AccountMutations = new ReadOnlyCollection<AccountStatus>(accountMutations);
            this.DataRecords = new ReadOnlyCollection<KeyValuePair<LedgerPath, BinaryData>>(dataRecords);
        }

        public IReadOnlyList<AccountStatus> AccountMutations { get; }

        public IReadOnlyList<KeyValuePair<LedgerPath, BinaryData>> DataRecords { get; }

        public static ParsedMutation Parse(Mutation mutation)
        {
            List<AccountStatus> accountMutations = new List<AccountStatus>();
            List<KeyValuePair<LedgerPath, BinaryData>> dataRecords = new List<KeyValuePair<LedgerPath, BinaryData>>();

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
                        case RecordType.Data:
                            dataRecords.Add(new KeyValuePair<LedgerPath, BinaryData>(
                                key.Path,
                                record.Value));
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

            return new ParsedMutation(accountMutations, dataRecords);
        }
    }
}
