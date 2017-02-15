using Newtonsoft.Json;
using Openchain.Validation.PermissionBased;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK.Models
{
    public class AclRecord
    {
        public AclRecord()
        {
            Subjects = new List<AclSubject>();
            RecordName = string.Empty;
            RecordNameMatching = PatternMatchingStrategy.Prefix.ToString();
            Recursive = true;

            Permissions = new Dictionary<string, string>();

            Permissions.Add("account_spend", Access.Unset.ToString());
            Permissions.Add("account_modify", Access.Unset.ToString());
            Permissions.Add("account_create", Access.Unset.ToString());
            Permissions.Add("account_negative", Access.Unset.ToString());
            Permissions.Add("data_modify", Access.Unset.ToString());
        }

        [JsonProperty("subjects")]
        public List<AclSubject> Subjects { get; set; }

        [JsonProperty("permissions")]
        public Dictionary<string, string> Permissions { get; set; }

        [JsonProperty("recursive")]
        public bool Recursive { get; set; }

        [JsonProperty("record_name")]
        public string RecordName { get; set; }

        [JsonProperty("record_name_matching")]
        public string RecordNameMatching { get; set; }

        public AclRecord AllowModify(bool allow = true)
        {
            AllowPermission("account_modify", allow);

            return this;
        }

        public AclRecord AllowCreate(bool allow = true)
        {
            AllowPermission("account_create", allow);
            return this;
        }

        public AclRecord AllowSpend(bool allow = true)
        {
            AllowPermission("account_spend", allow);
            return this;
        }

        public AclRecord AllowNegative(bool allow = true)
        {
            AllowPermission("account_negative", allow);
            return this;
        }

        public AclRecord AllowDataModify(bool allow = true)
        {
            AllowPermission("data_modify", allow);
            return this;
        }

        private void AllowPermission(string name, bool allow)
        {
            if(Permissions.ContainsKey(name))
            {
                Permissions.Remove(name);
            }

            Permissions.Add(name, allow ? Access.Permit.ToString() : Access.Deny.ToString());
        }
    }
}
