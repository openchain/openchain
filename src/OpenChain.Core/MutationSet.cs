using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenChain.Core
{
    public class MutationSet
    {
        public MutationSet(BinaryData @namespace, IEnumerable<Mutation> mutations, BinaryData metadata)
        {
            if (@namespace == null)
                throw new ArgumentNullException(nameof(@namespace));

            if (mutations == null)
                throw new ArgumentNullException(nameof(mutations));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Namespace = @namespace;
            this.Mutations = mutations.ToList().AsReadOnly();
            this.Metadata = metadata;

            if (this.Mutations.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(mutations));
        }

        public BinaryData Namespace { get; }

        public IReadOnlyList<Mutation> Mutations { get; }

        public BinaryData Metadata { get; }
    }
}
