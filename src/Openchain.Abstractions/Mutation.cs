// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Openchain
{
    /// <summary>
    /// Represent a mutation performed on a set of data records.
    /// </summary>
    public class Mutation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mutation"/> class.
        /// </summary>
        /// <param name="@namespace">The namespace in which the mutation operates.</param>
        /// <param name="records">A collection of all the records affected by the mutation.</param>
        /// <param name="metadata">The metadata associated with the mutation.</param>
        public Mutation(ByteString @namespace, IEnumerable<Record> records, ByteString metadata)
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

            // Records must not be null
            if (this.Records.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(records));

            // There must not be any duplicate keys
            HashSet<ByteString> keys = new HashSet<ByteString>();
            foreach (Record record in this.Records)
            {
                if (keys.Contains(record.Key))
                    throw new ArgumentNullException(nameof(records));

                keys.Add(record.Key);
            }
        }

        /// <summary>
        /// Gets the namespace in which the mutation operates.
        /// </summary>
        public ByteString Namespace { get; }

        /// <summary>
        /// Gets a collection of all the records affected by the mutation.
        /// </summary>
        public IReadOnlyList<Record> Records { get; }

        /// <summary>
        /// Gets the metadata associated with the mutation.
        /// </summary>
        public ByteString Metadata { get; }
    }
}
