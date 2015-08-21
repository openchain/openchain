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

namespace OpenChain
{
    /// <summary>
    /// Represent a mutation performed on a set of data records.
    /// </summary>
    public class Mutation
    {
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

            if (this.Records.Any(entry => entry == null))
                throw new ArgumentNullException(nameof(records));
        }

        /// <summary>
        /// Gets the namespace in which the mutation operates.
        /// </summary>
        public ByteString Namespace { get; }

        /// <summary>
        /// Gets a collection containing all the records being affected by the mutation.
        /// </summary>
        public IReadOnlyList<Record> Records { get; }

        /// <summary>
        /// Gets the metadata associated with the mutation.
        /// </summary>
        public ByteString Metadata { get; }
    }
}
