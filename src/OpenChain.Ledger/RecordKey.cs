using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenChain.Ledger
{
    public class RecordKey
    {
        public RecordKey(RecordType recordType, LedgerPath path, params LedgerPath[] additionalKeyComponents)
        {
            this.RecordType = recordType;
            this.Path = path;
            this.AdditionalKeyComponents = additionalKeyComponents.ToList().AsReadOnly();
        }

        public RecordType RecordType { get; }

        public LedgerPath Path { get; }

        public IReadOnlyList<LedgerPath> AdditionalKeyComponents { get; }

        public static RecordKey Parse(ByteString keyData)
        {
            if (keyData == null)
                throw new ArgumentNullException(nameof(keyData));

            byte[] key = keyData.ToByteArray();

            string[] parts = Encoding.UTF8.GetString(key, 0, key.Length).Split(':');
            if (parts.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(keyData));
            
            RecordKey result = ParseRecord(
                parts[1],
                LedgerPath.Parse(parts[0]),
                parts.Skip(2).Take(parts.Length - 2).Select(LedgerPath.Parse).ToList());
            
            // The byte representation of reencoded value must match the input
            if (!result.ToBinary().Equals(keyData))
                throw new ArgumentOutOfRangeException(nameof(keyData));

            return result;
        }

        public override string ToString()
        {
            if (AdditionalKeyComponents.Count > 0)
            {
                string additionalKeyComponents = string.Join(":", AdditionalKeyComponents.Select(item => item.FullPath));
                return $"{Path.FullPath}:{GetRecordTypeName(RecordType)}:{additionalKeyComponents}";
            }
            else
            {
                return $"{Path.FullPath}:{GetRecordTypeName(RecordType)}";
            }
        }

        public ByteString ToBinary() => new ByteString(Encoding.UTF8.GetBytes(ToString()));

        public static RecordKey ParseRecord(string recordType, LedgerPath ledgerPath, IList<LedgerPath> components)
        {
            switch (recordType)
            {
                case "ACC":
                    if (components.Count != 1)
                        throw new ArgumentOutOfRangeException(nameof(components));
                    return new RecordKey(RecordType.Account, ledgerPath, components[0]);
                case "DATA":
                    return new RecordKey(RecordType.Data, ledgerPath);
                default:
                    throw new ArgumentOutOfRangeException(nameof(recordType));
            }
        }

        public static string GetRecordTypeName(RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.Account:
                    return "ACC";
                case RecordType.Data:
                    return "DATA";
                default:
                    throw new ArgumentOutOfRangeException(nameof(recordType));
            }
        }
    }
}
