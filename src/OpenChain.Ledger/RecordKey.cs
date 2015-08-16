using System;
using System.Text;

namespace OpenChain.Ledger
{
    public class RecordKey
    {
        public RecordKey(RecordType recordType, LedgerPath path, string name)
        {
            this.RecordType = recordType;
            this.Path = path;
            this.Name = name;
        }

        public RecordType RecordType { get; }

        public LedgerPath Path { get; }

        public string Name { get; }

        public static RecordKey Parse(ByteString keyData)
        {
            if (keyData == null)
                throw new ArgumentNullException(nameof(keyData));

            byte[] key = keyData.ToByteArray();

            string[] parts = Encoding.UTF8.GetString(key, 0, key.Length).Split(new[] { ':' }, 3);
            if (parts.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(keyData));
            
            RecordKey result = ParseRecord(
                parts[1],
                LedgerPath.Parse(parts[0]),
                parts[2]);
            
            // The byte representation of reencoded value must match the input
            if (!result.ToBinary().Equals(keyData))
                throw new ArgumentOutOfRangeException(nameof(keyData));

            return result;
        }

        public override string ToString()
        {
            return $"{Path.FullPath}:{GetRecordTypeName(RecordType)}:{Name}";
        }

        public ByteString ToBinary() => new ByteString(Encoding.UTF8.GetBytes(ToString()));

        public static RecordKey ParseRecord(string recordType, LedgerPath ledgerPath, string name)
        {
            switch (recordType)
            {
                case "ACC":
                    LedgerPath path = LedgerPath.Parse(name);
                    return new RecordKey(RecordType.Account, ledgerPath, path.FullPath);
                case "DATA":
                    return new RecordKey(RecordType.Data, ledgerPath, name);
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
