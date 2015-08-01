using System;

namespace OpenChain
{
    /// <summary>
    /// Represents a data record identified by a key and a version, and containing a value.
    /// </summary>
    public class Record
    {
        public Record(BinaryData key, BinaryData value, BinaryData version)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (version == null)
                throw new ArgumentNullException(nameof(version));

            this.Key = key;
            this.Value = value;
            this.Version = version;
        }

        /// <summary>
        /// Gets the key of the record.
        /// </summary>
        public BinaryData Key { get; }

        /// <summary>
        /// Gets the value of the record.
        /// </summary>
        public BinaryData Value { get; }

        /// <summary>
        /// Gets the version of the record.
        /// </summary>
        public BinaryData Version { get; }
    }
}
