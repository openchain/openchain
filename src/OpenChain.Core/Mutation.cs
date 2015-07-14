using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenChain.Core
{
    public class Mutation
    {
        public Mutation(BinaryData @namespace, IEnumerable<KeyValuePair> keyValuePairs, BinaryData metadata)
        {
            if (@namespace == null)
                throw new ArgumentNullException(nameof(@namespace));

            if (keyValuePairs == null)
                throw new ArgumentNullException(nameof(keyValuePairs));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Namespace = @namespace;
            this.KeyValuePairs = keyValuePairs.ToList().AsReadOnly();
            this.Metadata = metadata;

            if (this.KeyValuePairs.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(keyValuePairs));
        }

        public BinaryData Namespace { get; }

        public IReadOnlyList<KeyValuePair> KeyValuePairs { get; }

        public BinaryData Metadata { get; }
    }
}
