using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenChain.Core
{
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

        public BinaryData Namespace { get; }

        public IReadOnlyList<Record> Records { get; }

        public BinaryData Metadata { get; }
    }
}
