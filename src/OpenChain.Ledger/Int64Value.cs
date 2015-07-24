using System;
using System.IO;

namespace OpenChain.Ledger
{
    public class Int64Value : BinaryValue
    {
        public Int64Value(BinaryValueUsage usage, long value)
            : base(usage)
        {
            this.Value = value;
            base.SetBinaryData();
        }

        public long Value { get; }

        public override BinaryValueType Type => BinaryValueType.Int64;

        protected override void Write(BinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}
