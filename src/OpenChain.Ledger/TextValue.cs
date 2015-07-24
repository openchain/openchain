using System;
using System.IO;
using System.Text;

namespace OpenChain.Ledger
{
    public class TextValue : BinaryValue
    {
        public TextValue(BinaryValueUsage usage, string value)
            : base(usage)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            this.Value = value;
            base.SetBinaryData();
        }

        public string Value { get; }

        public override BinaryValueType Type => BinaryValueType.String;

        protected override void Write(BinaryWriter writer)
        {
            byte[] value = Encoding.UTF8.GetBytes(Value);
            writer.Write((uint)value.Length);
            writer.Write(value);
        }
    }
}
