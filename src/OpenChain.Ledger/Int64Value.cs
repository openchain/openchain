using System.IO;

namespace OpenChain.Ledger
{
    public class Int64Value : BinaryValue
    {
        public Int64Value(long value)
            : base(BinaryValueUsage.Int64)
        {
            this.Value = value;
            base.SetBinaryData();
        }

        public long Value { get; }

        protected override void Write(BinaryWriter writer)
        {
            writer.Write((int)Usage);
            writer.Write(Value);
        }
    }
}
