using System;
using System.IO;

namespace OpenChain.Ledger
{
    public class TextValue : BinaryValue
    {
        public TextValue(string value)
            : base(BinaryValueUsage.Text)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            this.Value = value;
            base.SetBinaryData();
        }

        public string Value { get; }

        protected override void Write(BinaryWriter writer)
        {
            writer.Write((int)Usage);
            writer.Write(Value);
        }
    }
}
