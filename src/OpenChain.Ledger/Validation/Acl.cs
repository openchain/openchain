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

namespace OpenChain.Ledger.Validation
{
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

        public IReadOnlyList<IPermissionSubject> Subjects { get; }

        public LedgerPath Path { get; }

        public bool Recursive { get; }

        public StringPattern RecordName { get; }

        public PermissionSet Permissions { get; }

        public static IReadOnlyList<Acl> Parse(string json, KeyEncoder keyEncoder)
        {
            JArray document = JArray.Parse(json);

            return ((IEnumerable<JToken>)document).Select(root =>
                new Acl(
                    ((JArray)root["subjects"]).Children().Select(subject =>
                        new P2pkhSubject(new[] { (string)subject["key"] }, (int)subject["required"], keyEncoder)),
                    LedgerPath.Parse((string)root["path"]),
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

        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, bool recursiveOnly, string recordName)
        {
            return Path.FullPath == path.FullPath
                && (Path.Segments.Count == path.Segments.Count || Recursive)
                && (!recursiveOnly || Recursive)
                && RecordName.IsMatch(recordName)
                && Subjects.Any(subject => subject.IsMatch(authentication));
        }
    }
}
