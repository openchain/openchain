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
            return Path.IsParentOf(path)
                && (Path.Segments.Count == path.Segments.Count || Recursive)
                && (!recursiveOnly || Recursive)
                && RecordName.IsMatch(recordName)
                && Subjects.Any(subject => subject.IsMatch(authentication));
        }
    }
}
