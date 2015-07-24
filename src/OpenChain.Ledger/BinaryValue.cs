using System;
using System.IO;
using System.Text;
using OpenChain.Core;

namespace OpenChain.Ledger
{
    public abstract class BinaryValue : IEquatable<BinaryValue>
    {
        public static BinaryValue Default { get; private set; } = new DefaultValue();

        public BinaryData BinaryData { get; private set; }

        public BinaryValueUsage Usage { get; }

        public abstract BinaryValueType Type { get; }

        public BinaryValue(BinaryValueUsage usage)
        {
            this.Usage = usage;
        }

        protected abstract void Write(BinaryWriter writer);

        protected void SetBinaryData()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                if (Usage != BinaryValueUsage.None)
                    writer.Write((ushort)Usage);

                writer.Write((byte)Type);

                Write(writer);

                BinaryData = new BinaryData(stream.ToArray());
            }
        }

        public static BinaryValue Read(BinaryData key, bool isKey)
        {
            BinaryValue result;
            if (key.Value.Count == 0)
                return Default;

            try
            {
                using (Stream input = key.ToStream())
                using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8))
                {
                    BinaryValueUsage usage;
                    if (isKey)
                        usage = (BinaryValueUsage)reader.ReadUInt16();
                    else
                        usage = BinaryValueUsage.None;

                    BinaryValueType type = (BinaryValueType)reader.ReadByte();


                    switch (type)
                    {
                        case BinaryValueType.StringPair:
                            uint accountLength = reader.ReadUInt32();
                            string account = Encoding.UTF8.GetString(reader.ReadBytes((int)accountLength));
                            uint assetLength = reader.ReadUInt32();
                            string asset = Encoding.UTF8.GetString(reader.ReadBytes((int)assetLength));
                            result = new AccountKey(usage, account, asset);
                            break;
                        case BinaryValueType.String:
                            uint stringLength = reader.ReadUInt32();
                            string value = Encoding.UTF8.GetString(reader.ReadBytes((int)stringLength));
                            result = new TextValue(usage, value);
                            break;
                        case BinaryValueType.Int64:
                            long intValue = reader.ReadInt64();
                            result = new Int64Value(usage, intValue);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (input.Position != input.Length)
                        throw new ArgumentOutOfRangeException();
                    else if (!result.BinaryData.Equals(key))
                        throw new ArgumentOutOfRangeException();
                    else
                        return result;
                }
            }
            catch (EndOfStreamException)
            {
                throw new ArgumentOutOfRangeException();
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

        private class DefaultValue : BinaryValue
        {
            public DefaultValue()
                : base(BinaryValueUsage.None)
            {
                BinaryData = BinaryData.Empty;
            }

            public override BinaryValueType Type => BinaryValueType.Default;

            protected override void Write(BinaryWriter writer)
            { }
        }
    }
}
