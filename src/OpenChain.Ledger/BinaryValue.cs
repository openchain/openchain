using OpenChain.Core;
using System;
using System.IO;
using System.Text;

namespace OpenChain.Ledger
{
    public abstract class BinaryValue : IEquatable<BinaryValue>
    {
        public BinaryValue(BinaryValueUsage usage)
        {
            this.Usage = usage;
        }

        public BinaryData BinaryData { get; private set; }

        public BinaryValueUsage Usage { get; }

        protected abstract void Write(BinaryWriter writer);

        protected void SetBinaryData()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                Write(writer);

                BinaryData = new BinaryData(stream.ToArray());
            }
        }

        public static BinaryValue Read(BinaryData key)
        {
            using (Stream input = key.ToStream())
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8))
            {
                BinaryValueUsage type = (BinaryValueUsage)reader.ReadInt32();
                BinaryValue result;

                switch (type)
                {
                    case BinaryValueUsage.AccountKey:
                        string account = reader.ReadString();
                        string asset = reader.ReadString();
                        result = new AccountKey(account, asset);
                        break;
                    case BinaryValueUsage.Text:
                        string stringValue = reader.ReadString();
                        result = new TextValue(stringValue);
                        break;
                    case BinaryValueUsage.Int64:
                        long intValue = reader.ReadInt64();
                        result = new Int64Value(intValue);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (input.Position != input.Length)
                    throw new ArgumentOutOfRangeException();
                else
                    return result;
            }
        }

        public bool Equals(BinaryValue other)
        {
            if (other == null)
                return false;
            else
                return this.BinaryData.Equals(other.BinaryData);
        }

        public override int GetHashCode()
        {
            return this.BinaryData.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BinaryValue);
        }
    }
}
