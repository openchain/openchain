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
using System.Text;
using System.Threading.Tasks;
using Openchain.Ledger.Validation;
using Xunit;

namespace Openchain.Ledger.Tests
{
    public class DynamicPermissionLayoutTests
    {
        private static readonly SignatureEvidence[] evidence = new[] { new SignatureEvidence(ByteString.Parse("abcdef"), ByteString.Empty) };

        [Fact]
        public async Task GetPermissions_Match()
        {
            DynamicPermissionLayout layout = new DynamicPermissionLayout(new TestStore(), new KeyEncoder(111));

            PermissionSet result = await layout.GetPermissions(evidence, LedgerPath.Parse("/root/subitem/"), true, "name");

            Assert.Equal(Access.Permit, result.AccountModify);
            Assert.Equal(Access.Permit, result.AccountNegative);
            Assert.Equal(Access.Permit, result.AccountSpend);
            Assert.Equal(Access.Permit, result.DataModify);
        }

        [Fact]
        public async Task GetPermissions_NoMatch()
        {
            DynamicPermissionLayout layout = new DynamicPermissionLayout(new TestStore(), new KeyEncoder(111));

            PermissionSet result = await layout.GetPermissions(evidence, LedgerPath.Parse("/root/subitem/"), true, "other");

            Assert.Equal(Access.Unset, result.AccountModify);
            Assert.Equal(Access.Unset, result.AccountNegative);
            Assert.Equal(Access.Unset, result.AccountSpend);
            Assert.Equal(Access.Unset, result.DataModify);
        }

        [Fact]
        public async Task GetPermissions_NoAcl()
        {
            DynamicPermissionLayout layout = new DynamicPermissionLayout(new TestStore(), new KeyEncoder(111));

            PermissionSet result = await layout.GetPermissions(evidence, LedgerPath.Parse("/root/other/"), true, "name");

            Assert.Equal(Access.Unset, result.AccountModify);
            Assert.Equal(Access.Unset, result.AccountNegative);
            Assert.Equal(Access.Unset, result.AccountSpend);
            Assert.Equal(Access.Unset, result.DataModify);
        }

        [Fact]
        public async Task GetPermissions_InvalidAcl()
        {
            DynamicPermissionLayout layout = new DynamicPermissionLayout(new TestStore(), new KeyEncoder(111));

            PermissionSet result = await layout.GetPermissions(evidence, LedgerPath.Parse("/root/invalid/"), true, "name");

            Assert.Equal(Access.Unset, result.AccountModify);
            Assert.Equal(Access.Unset, result.AccountNegative);
            Assert.Equal(Access.Unset, result.AccountSpend);
            Assert.Equal(Access.Unset, result.DataModify);
        }

        [Fact]
        public async Task GetPermissions_JsonComments()
        {
            DynamicPermissionLayout layout = new DynamicPermissionLayout(new TestStore(), new KeyEncoder(111));

            PermissionSet result = await layout.GetPermissions(evidence, LedgerPath.Parse("/root/comment/"), true, "name");

            Assert.Equal(Access.Unset, result.AccountModify);
            Assert.Equal(Access.Unset, result.AccountNegative);
            Assert.Equal(Access.Unset, result.AccountSpend);
            Assert.Equal(Access.Unset, result.DataModify);
        }

        private class TestStore : IStorageEngine
        {
            public Task AddTransactions(IEnumerable<ByteString> transactions)
            {
                throw new NotImplementedException();
            }

            public Task<ByteString> GetLastTransaction()
            {
                throw new NotImplementedException();
            }

            public Task<IList<Record>> GetRecords(IEnumerable<ByteString> keys)
            {
                return Task.FromResult<IList<Record>>(keys.Select(key =>
                {
                    RecordKey recordKey = RecordKey.Parse(key);

                    if (recordKey.Name == "acl")
                    {
                        if (recordKey.Path.FullPath == "/root/subitem/")
                        {
                            return new Record(key, GetValidAcl(), ByteString.Empty);
                        }
                        else if (recordKey.Path.FullPath == "/root/invalid/")
                        {
                            return new Record(key, GetInvalidAcl(), ByteString.Empty);
                        }
                        else if (recordKey.Path.FullPath == "/root/comment/")
                        {
                            return new Record(key, GetCommentedAcl(), ByteString.Empty);
                        }
                    }

                    return new Record(key, ByteString.Empty, ByteString.Empty);
                })
                .ToList());
            }

            private static ByteString GetValidAcl()
            {
                return new ByteString(
                    Encoding.UTF8.GetBytes(@"
                        [{
                            ""subjects"": [ { ""addresses"": [ ""n12RA1iohYEerfXiBixSoERZG8TP8xQFL2"" ], ""required"": 1 } ],
                            ""recursive"": ""true"",
                            ""record_name"": ""name"",
                            ""record_name_matching"": ""Exact"",
                            ""permissions"": {
                                ""account_negative"": ""Permit"",
                                ""account_spend"": ""Permit"",
                                ""account_modify"": ""Permit"",
                                ""data_modify"": ""Permit""
                            }
                        }]
                    "));
            }

            private static ByteString GetInvalidAcl()
            {
                return new ByteString(
                    Encoding.UTF8.GetBytes(@"[{ ""invalid"" }]"));
            }

            private static ByteString GetCommentedAcl()
            {
                return new ByteString(
                    Encoding.UTF8.GetBytes(@"[ /* Comment */ { }]"));
            }

            public IObservable<ByteString> GetTransactionStream(ByteString from)
            {
                throw new NotImplementedException();
            }
        }
    }
}
