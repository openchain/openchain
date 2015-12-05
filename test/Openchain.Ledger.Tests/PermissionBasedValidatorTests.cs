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
using System.Threading.Tasks;
using Openchain.Ledger.Validation;
using Xunit;

namespace Openchain.Ledger.Tests
{
    public class PermissionBasedValidatorTests
    {
        [Fact]
        public async Task Validate_ComputeAddress()
        {
            PermissionBasedValidator validator = CreateValidator(
                new string[] { "0123456789abcdef11223344" },
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.Unset,
                    ["/a/"] = PermissionSet.AllowAll
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>()
            {
                [AccountKey.Parse("/a/", "/b/")] = new AccountStatus(AccountKey.Parse("/a/", "/b/"), 150, ByteString.Empty)
            };

            ParsedMutation mutation = new ParsedMutation(
                new[] { new AccountStatus(AccountKey.Parse("/a/", "/b/"), 100, ByteString.Empty) },
                new KeyValuePair<RecordKey, ByteString>[] { });

            await validator.Validate(
                mutation,
                new[] { new SignatureEvidence(ByteString.Parse("0123456789abcdef"), ByteString.Parse("11223344")) },
                accounts);
        }

        [Fact]
        public async Task Validate_AccountMutation()
        {
            TransactionInvalidException exception;

            // Able to spend existing funds as the issuer
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Deny, Access.Permit, Access.Deny, Access.Deny),
                previousBalance: 150,
                newBalance: 100);

            // Able to spend non-existing funds as the issuer
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Deny, Access.Permit, Access.Deny, Access.Deny),
                previousBalance: 100,
                newBalance: -50);

            // Able to spend funds as the owner
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Permit, Access.Permit, Access.Deny, Access.Deny),
                previousBalance: 100,
                newBalance: 50);

            // Able to receive funds
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Deny, Access.Permit, Access.Deny, Access.Deny),
                previousBalance: 50,
                newBalance: 100);

            // Missing the affect balance permission
            exception = await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Permit, Access.Deny, Access.Permit, Access.Permit),
                previousBalance: 100,
                newBalance: 150));
            Assert.Equal("AccountModificationUnauthorized", exception.Reason);

            // Missing the permissions to spend from the account
            exception = await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Deny, Access.Permit, Access.Permit, Access.Permit),
                previousBalance: 150,
                newBalance: 100));
            Assert.Equal("CannotSpendFromAccount", exception.Reason);

            // Not able to spend more than the funds on the account
            exception = await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Permit, Access.Permit, Access.Permit, Access.Permit),
                previousBalance: 100,
                newBalance: -50));
            Assert.Equal("CannotIssueAsset", exception.Reason);
        }

        [Fact]
        public async Task Validate_DataMutationSuccess()
        {
            PermissionBasedValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.Unset,
                    ["/a/"] = new PermissionSet(Access.Deny, Access.Deny, Access.Deny, Access.Deny, Access.Permit)
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>();

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<RecordKey, ByteString>(new RecordKey(RecordType.Data, LedgerPath.Parse("/a/"), "a"), ByteString.Parse("aabb")) });

            await validator.Validate(mutation, new SignatureEvidence[0], accounts);
        }

        [Fact]
        public async Task Validate_DataMutationError()
        {
            PermissionBasedValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.Unset,
                    ["/a/"] = new PermissionSet(Access.Permit, Access.Permit, Access.Permit, Access.Deny)
                });

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<RecordKey, ByteString>(new RecordKey(RecordType.Data, LedgerPath.Parse("/a/"), "a"), ByteString.Parse("aabb")) });

            TransactionInvalidException exception = await Assert.ThrowsAsync<TransactionInvalidException>(() =>
                validator.Validate(mutation, new SignatureEvidence[0], new Dictionary<AccountKey, AccountStatus>()));
            Assert.Equal("CannotModifyData", exception.Reason);
        }

        [Fact]
        public async Task Validate_Inheritance()
        {
            TestPermissionsProvider firstValidator = new TestPermissionsProvider(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.AllowAll,
                    ["/a/"] = PermissionSet.AllowAll
                });

            TestPermissionsProvider secondValidator = new TestPermissionsProvider(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.DenyAll,
                    ["/a/"] = PermissionSet.Unset
                });

            // Level 1: /
            //   Allow + Deny = Deny
            // Level 2: /a/
            //   Allow + Unset = Allow
            // Result: Allow
            PermissionBasedValidator validator = new PermissionBasedValidator(new[] { firstValidator, secondValidator });

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<RecordKey, ByteString>(new RecordKey(RecordType.Data, LedgerPath.Parse("/a/"), "a"), ByteString.Parse("aabb")) });

            await validator.Validate(mutation, new SignatureEvidence[0], new Dictionary<AccountKey, AccountStatus>());
        }

        private static async Task TestAccountChange(PermissionSet accountPermissions, long previousBalance, long newBalance)
        {
            PermissionBasedValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/"] = PermissionSet.Unset,
                    ["/a/"] = accountPermissions
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>()
            {
                [AccountKey.Parse("/a/", "/b/")] = new AccountStatus(AccountKey.Parse("/a/", "/b/"), previousBalance, ByteString.Empty)
            };

            ParsedMutation mutation = new ParsedMutation(
                new[] { new AccountStatus(AccountKey.Parse("/a/", "/b/"), newBalance, ByteString.Empty) },
                new KeyValuePair<RecordKey, ByteString>[0]);

            await validator.Validate(mutation, new SignatureEvidence[0], accounts);
        }

        private static PermissionBasedValidator CreateValidator(IList<string> identities, Dictionary<string, PermissionSet> getPermissions)
        {
            TestPermissionsProvider permissions = new TestPermissionsProvider(identities, getPermissions);
            return new PermissionBasedValidator(new[] { permissions });
        }

        private class TestPermissionsProvider : IPermissionsProvider
        {
            private readonly IList<string> expectedIdentities;
            private readonly Dictionary<string, PermissionSet> getPermissions;

            public TestPermissionsProvider(IList<string> expectedIdentities, Dictionary<string, PermissionSet> getPermissions)
            {
                this.expectedIdentities = expectedIdentities;
                this.getPermissions = getPermissions;
            }

            public Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path, bool recursiveOnly, string recordName)
            {
                Assert.Equal(identities.Select(ConvertEvidence), expectedIdentities, StringComparer.Ordinal);

                PermissionSet result;
                if (!getPermissions.TryGetValue(path.FullPath, out result))
                    throw new InvalidOperationException();
                else
                    return Task.FromResult(result);
            }

            private static string ConvertEvidence(SignatureEvidence pubKey)
            {
                return pubKey.PublicKey.ToString() + pubKey.Signature.ToString();
            }
        }
    }
}
