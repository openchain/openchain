using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpenChain.Ledger.Validation
{
    public class Acl
    {
        public Acl(
            IEnumerable<P2pkhSubject> subjects,
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

        public IReadOnlyList<P2pkhSubject> Subjects { get; }

        public LedgerPath Path { get; }

        public bool Recursive { get; }

        public StringPattern RecordName { get; }

        public PermissionSet Permissions { get; }

        public static Acl Parse(string json, KeyEncoder keyEncoder)
        {
            JObject root = JObject.Parse(json);

            return new Acl(
                ((JArray)root["subjects"]).Children().Select(subject =>
                    new P2pkhSubject(new[] { (string)subject["key"] }, (int)subject["required"], keyEncoder)),
                LedgerPath.Parse((string)root["path"]),
                (bool)root["recursive"],
                new StringPattern((string)root["record_name"], (PatternMatchingStrategy)Enum.Parse(typeof(PatternMatchingStrategy), (string)root["record_name_matching"])),
                new PermissionSet(
                    accountNegative: (bool)root["permissions"]["account_negative"],
                    accountSpend: (bool)root["permissions"]["account_spend"],
                    accountModify: (bool)root["permissions"]["account_modify"],
                    dataModify: (bool)root["permissions"]["data_modify"]));
        }

        public bool IsMatch(IReadOnlyList<SignatureEvidence> authentication, LedgerPath path, string recordName)
        {
            return Path.IsParentOf(path)
                && (Path.Segments.Count == path.Segments.Count || Recursive)
                && RecordName.IsMatch(recordName)
                && Subjects.Any(subject => subject.IsMatch(authentication));
        }
    }
}
