using System;

namespace OpenChain.Core
{
    public class KeyValuePair
    {
        public KeyValuePair(BinaryData key, BinaryData value, BinaryData version)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (version == null)
                throw new ArgumentNullException(nameof(version));

            this.Key = key;
            this.Value = value;
            this.Version = version;
        }

        public BinaryData Key { get; }

        public BinaryData Value { get; }

        public BinaryData Version { get; }
    }
}
