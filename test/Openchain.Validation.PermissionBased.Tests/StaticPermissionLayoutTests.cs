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

using System.Threading.Tasks;
using Openchain.Infrastructure;
using Xunit;

namespace Openchain.Validation.PermissionBased.Tests
{
    public class StaticPermissionLayoutTests
    {
        private static readonly P2pkhSubject subject = new P2pkhSubject(new[] { "n12RA1iohYEerfXiBixSoERZG8TP8xQFL2" }, 1, new KeyEncoder(111));
        private static readonly SignatureEvidence[] evidence = new[] { new SignatureEvidence(ByteString.Parse("abcdef"), ByteString.Empty) };
        private static readonly LedgerPath path = LedgerPath.Parse("/root/subitem/");
        private static readonly PermissionSet permissions = PermissionSet.AllowAll;

        [Fact]
        public async Task GetPermissions_Match()
        {
            StaticPermissionLayout layout = new StaticPermissionLayout(new[]
            {
                new Acl(new[] { subject }, path, true, new StringPattern("name", PatternMatchingStrategy.Exact), permissions)
            });

            PermissionSet result = await layout.GetPermissions(evidence, path, true, "name");

            Assert.Equal(Access.Permit, result.AccountModify);
            Assert.Equal(Access.Permit, result.AccountNegative);
            Assert.Equal(Access.Permit, result.AccountSpend);
            Assert.Equal(Access.Permit, result.DataModify);
        }

        [Fact]
        public async Task GetPermissions_NoMatch()
        {
            StaticPermissionLayout layout = new StaticPermissionLayout(new[]
            {
                new Acl(new[] { subject }, path, true, new StringPattern("name", PatternMatchingStrategy.Exact), permissions)
            });

            PermissionSet result = await layout.GetPermissions(evidence, path, true, "other_name");

            Assert.Equal(Access.Unset, result.AccountModify);
            Assert.Equal(Access.Unset, result.AccountNegative);
            Assert.Equal(Access.Unset, result.AccountSpend);
            Assert.Equal(Access.Unset, result.DataModify);
        }
    }
}
