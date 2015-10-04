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
using Newtonsoft.Json.Linq;

namespace Openchain.Ledger.Validation
{
    /// <summary>
    /// Represents an access control rule.
    /// </summary>
    public class Acl
    {
        public Acl(
            IEnumerable<IPermissionSubject> subjects,
            LedgerPath path,
            bool recursive,
            StringPattern recordName,
            PermissionSet permissions)
        {
            this.Subjects = subjects.ToList().AsReadOnly();
            this.Path = path;
            this.Recursive = recursive;
            this.RecordName = recordName;
            this.Permissions = permissions;
        }

        /// <summary>
        /// Gets a read-only list of all the subjects of this permission.
        /// </summary>
        public IReadOnlyList<IPermissionSubject> Subjects { get; }

        /// <summary>
        /// Gets the path to which this permission applies.
        /// </summary>
        public LedgerPath Path { get; }

        /// <summary>
        /// Gets a boolean indicating whether this permission applies recursively.
        /// </summary>
        public bool Recursive { get; }

        /// <summary>
        /// Gets a <see cref="StringPattern"/> that matches record names of records to which this permission applies.
        /// </summary>
        public StringPattern RecordName { get; }

        /// <summary>
        /// Gets the <see cref="PermissionSet"/> being applied.
        /// </summary>
        public PermissionSet Permissions { get; }

        /// <summary>
        /// Parses a permission set from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <param name="path">The path on which these permissions apply.</param>
        /// <param name="keyEncoder">The key encoder to use in the parsed <see cref="Acl"/> objects.</param>
        /// <returns>The parsed list of <see cref="Acl"/> objects.</returns>
        public static IReadOnlyList<Acl> Parse(string json, LedgerPath path, KeyEncoder keyEncoder)
        {
            JArray document = JArray.Parse(json);

            return ((IEnumerable<JToken>)document).Select(root =>
                new Acl(
                    ((JArray)root["subjects"]).Children().Select(subject =>
                        new P2pkhSubject(((JArray)subject["addresses"]).Select(key => (string)key), (int)subject["required"], keyEncoder)),
                    path,
                    (bool)root["recursive"],
                    new StringPattern((string)root["record_name"], (PatternMatchingStrategy)Enum.Parse(typeof(PatternMatchingStrategy), (string)root["record_name_matching"])),
                    new PermissionSet(
                        accountNegative: Parse(root["permissions"]["account_negative"]),
                        accountSpend: Parse(root["permissions"]["account_spend"]),
                        accountModify: Parse(root["permissions"]["account_modify"]),
                        dataModify: Parse(root["permissions"]["data_modify"]))))
                .ToList();
        }

        private static Access Parse(JToken value)
        {
            return (Access)Enum.Parse(typeof(Access), (string)value);
        }

        /// <summary>
        /// Checks if the given parameters constitute a match for this <see cref="Acl"/> object.
        /// </summary>
        /// <param name="authentication">The signatures available.</param>
        /// <param name="path">The path of the record being tested.</param>
        /// <param name="recursiveOnly">Whether this object must be a recursive permission.</param>
        /// <param name="recordName">The name of the record being tested.</param>
        /// <returns>A boolean indicating whether the given parameters match.</returns>
        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, bool recursiveOnly, string recordName)
        {
            return Path.FullPath == path.FullPath
                && (!recursiveOnly || Recursive)
                && RecordName.IsMatch(recordName)
                && Subjects.Any(subject => subject.IsMatch(authentication));
        }
    }
}
