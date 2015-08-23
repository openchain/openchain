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

using OpenChain.Ledger.Validation;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class PermissionSetTests
    {
        [Fact]
        public void Add_AllCombinations()
        {
            AssertPermissionSet(Access.Permit, PermissionSet.AllowAll.Add(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.AllowAll.Add(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Permit, PermissionSet.AllowAll.Add(PermissionSet.Unset));
            AssertPermissionSet(Access.Deny, PermissionSet.DenyAll.Add(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.DenyAll.Add(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Deny, PermissionSet.DenyAll.Add(PermissionSet.Unset));
            AssertPermissionSet(Access.Permit, PermissionSet.Unset.Add(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.Unset.Add(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Unset, PermissionSet.Unset.Add(PermissionSet.Unset));
        }

        [Fact]
        public void AddLevel_AllCombinations()
        {
            AssertPermissionSet(Access.Permit, PermissionSet.AllowAll.AddLevel(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.AllowAll.AddLevel(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Permit, PermissionSet.AllowAll.AddLevel(PermissionSet.Unset));
            AssertPermissionSet(Access.Permit, PermissionSet.DenyAll.AddLevel(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.DenyAll.AddLevel(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Deny, PermissionSet.DenyAll.AddLevel(PermissionSet.Unset));
            AssertPermissionSet(Access.Permit, PermissionSet.Unset.AddLevel(PermissionSet.AllowAll));
            AssertPermissionSet(Access.Deny, PermissionSet.Unset.AddLevel(PermissionSet.DenyAll));
            AssertPermissionSet(Access.Unset, PermissionSet.Unset.AddLevel(PermissionSet.Unset));
        }

        private void AssertPermissionSet(Access expected, PermissionSet permissions)
        {
            Assert.Equal(expected, permissions.AccountModify);
            Assert.Equal(expected, permissions.AccountNegative);
            Assert.Equal(expected, permissions.AccountSpend);
            Assert.Equal(expected, permissions.DataModify);
        }
    }
}
