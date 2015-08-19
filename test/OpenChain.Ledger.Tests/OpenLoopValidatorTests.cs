using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenChain.Ledger.Validation;
using Xunit;

namespace OpenChain.Ledger.Tests
{
    public class OpenLoopValidatorTests
    {
        [Fact]
        public async Task Validate_ComputeAddress()
        {
            OpenLoopValidator validator = CreateValidator(
                new string[] { "0123456789abcdef11223344" },
                new Dictionary<string, PermissionSet>()
                {
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
            // Able to spend existing funds as the issuer
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Deny, Access.Permit, Access.Deny),
                previousBalance: 150,
                newBalance: 100);

            // Able to spend non-existing funds as the issuer
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Deny, Access.Permit, Access.Deny),
                previousBalance: 100,
                newBalance: -50);

            // Able to spend funds as the owner
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Permit, Access.Permit, Access.Deny),
                previousBalance: 100,
                newBalance: 50);

            // Able to receive funds
            await TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Deny, Access.Permit, Access.Deny),
                previousBalance: 50,
                newBalance: 100);

            // Missing the affect balance permission
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Permit, Access.Permit, Access.Deny, Access.Permit),
                previousBalance: 100,
                newBalance: 150));

            // Missing the permissions to spend from the account
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Deny, Access.Permit, Access.Permit),
                previousBalance: 150,
                newBalance: 100));

            // Not able to spend more that the funds on the account
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                accountPermissions: new PermissionSet(Access.Deny, Access.Permit, Access.Permit, Access.Permit),
                previousBalance: 100,
                newBalance: -50));
        }

        [Fact]
        public async Task Validate_DataMutationSuccess()
        {
            OpenLoopValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/a/"] = new PermissionSet(Access.Deny, Access.Deny, Access.Deny, Access.Permit)
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
            OpenLoopValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/a/"] = new PermissionSet(Access.Permit, Access.Permit, Access.Permit, Access.Deny)
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>();

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<RecordKey, ByteString>(new RecordKey(RecordType.Data, LedgerPath.Parse("/a/"), "a"), ByteString.Parse("aabb")) });

            await Assert.ThrowsAsync<TransactionInvalidException>(() => validator.Validate(mutation, new SignatureEvidence[0], accounts));
        }

        private static async Task TestAccountChange(PermissionSet accountPermissions, long previousBalance, long newBalance)
        {
            OpenLoopValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
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

        private static OpenLoopValidator CreateValidator(IList<string> identities, Dictionary<string, PermissionSet> getPermissions)
        {
            TestPermissionsProvider permissions = new TestPermissionsProvider(identities, getPermissions);
            return new OpenLoopValidator(new[] { permissions });
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
