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
using System.Text;

namespace OpenChain.Ledger
{
    /// <summary>
    /// Represents the key for a record.
    /// </summary>
    public class RecordKey
    {
        public RecordKey(RecordType recordType, LedgerPath path, string name)
        {
            this.RecordType = recordType;
            this.Path = path;
            this.Name = name;
        }

        /// <summary>
        /// Gets the type of record.
        /// </summary>
        public RecordType RecordType { get; }

        /// <summary>
        /// Gets the path of the record.
        /// </summary>
        public LedgerPath Path { get; }

        /// <summary>
        /// Gets the record name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parses a <see cref="ByteString"/> into an instance of a <see cref="RecordKey"/> object.
        /// </summary>
        /// <param name="keyData">The <see cref="ByteString"/> to parse.</param>
        /// <returns>The parsed <see cref="RecordKey"/>.</returns>
        public static RecordKey Parse(ByteString keyData)
        {
            if (keyData == null)
                throw new ArgumentNullException(nameof(keyData));

            byte[] key = keyData.ToByteArray();

            string[] parts = Encoding.UTF8.GetString(key, 0, key.Length).Split(new[] { ':' }, 3);
            if (parts.Length != 3)
                throw new ArgumentOutOfRangeException(nameof(keyData));
            
            RecordKey result = ParseRecord(
                parts[1],
                LedgerPath.Parse(parts[0]),
                parts[2]);
            
            // The byte representation of reencoded value must match the input
            if (!result.ToBinary().Equals(keyData))
                throw new ArgumentOutOfRangeException(nameof(keyData));

            return result;
        }

        public override string ToString()
        {
            return $"{Path.FullPath}:{GetRecordTypeName(RecordType)}:{Name}";
        }

        /// <summary>
        /// Returns the byte representation of the current object.
        /// </summary>
        /// <returns>A <see cref="ByteString"/> representing the current object.</returns>
        public ByteString ToBinary() => new ByteString(Encoding.UTF8.GetBytes(ToString()));

        public static RecordKey ParseRecord(string recordType, LedgerPath ledgerPath, string name)
        {
            switch (recordType)
            {
                case "ACC":
                    LedgerPath path = LedgerPath.Parse(name);
                    return new RecordKey(RecordType.Account, ledgerPath, path.FullPath);
                case "DATA":
                    return new RecordKey(RecordType.Data, ledgerPath, name);
                default:
                    throw new ArgumentOutOfRangeException(nameof(recordType));
            }
        }

        public static string GetRecordTypeName(RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.Account:
                    return "ACC";
                case RecordType.Data:
                    return "DATA";
                default:
                    throw new ArgumentOutOfRangeException(nameof(recordType));
            }
        }
    }
}
