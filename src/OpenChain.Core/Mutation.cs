using System;

namespace OpenChain.Core
{
    public class Mutation
    {
        public Mutation(BinaryData key, BinaryData value, BinaryData version)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

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
