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
