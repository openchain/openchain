using System;

namespace OpenChain
{
    /// <summary>
    /// Represents a data record identified by a key and a version, and containing a value.
    /// </summary>
    public class Record
    {
        public Record(ByteString key, ByteString value, ByteString version)
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
        public ByteString Key { get; }

        /// <summary>
        /// Gets the value of the record.
        /// </summary>
        public ByteString Value { get; }

        /// <summary>
        /// Gets the version of the record.
        /// </summary>
        public ByteString Version { get; }
    }
}
