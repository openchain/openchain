using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenChain
{
    /// <summary>
    /// Represent a mutation performed on a set of data records.
    /// </summary>
    public class Mutation
    {
        public Mutation(BinaryData @namespace, IEnumerable<Record> records, BinaryData metadata)
        {
            if (@namespace == null)
                throw new ArgumentNullException(nameof(@namespace));

            if (records == null)
                throw new ArgumentNullException(nameof(records));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Namespace = @namespace;
            this.Records = records.ToList().AsReadOnly();
            this.Metadata = metadata;

            if (this.Records.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(records));
        }

        /// <summary>
        /// Gets the namespace in which the mutation operates.
        /// </summary>
        public BinaryData Namespace { get; }

        /// <summary>
        /// Gets a collection containing all the records being affected by the mutation.
        /// </summary>
        public IReadOnlyList<Record> Records { get; }

        /// <summary>
        /// Gets the metadata associated with the mutation.
        /// </summary>
        public BinaryData Metadata { get; }
    }
}
