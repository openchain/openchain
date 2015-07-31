using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    ["/a"] = new PermissionSet(true, true, true, true),
                    ["/b"] = new PermissionSet(true, true, true, true)
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>()
            {
                [AccountKey.Parse("/a", "/b")] = new AccountStatus(AccountKey.Parse("/a", "/b"), 150, BinaryData.Empty)
            };

            ParsedMutation mutation = new ParsedMutation(
                new[] { new AccountStatus(AccountKey.Parse("/a", "/b"), 100, BinaryData.Empty) },
                new KeyValuePair<LedgerPath, BinaryData>[] { });

            await validator.Validate(
                mutation,
                new[] { new SignatureEvidence(BinaryData.Parse("0123456789abcdef"), BinaryData.Parse("11223344")) },
                accounts);
        }

        [Fact]
        public async Task Validate_AccountMutation()
        {
            // Able to spend existing funds as the issuer
            await TestAccountChange(
                issuancePermissions: new PermissionSet(true, false, false, false),
                accountPermissions: new PermissionSet(false, false, true, false),
                previousBalance: 150,
                newBalance: 100);

            // Able to spend non-existing funds as the issuer
            await TestAccountChange(
                issuancePermissions: new PermissionSet(true, false, false, false),
                accountPermissions: new PermissionSet(false, false, true, false),
                previousBalance: 100,
                newBalance: -50);

            // Able to spend funds as the owner
            await TestAccountChange(
                issuancePermissions: new PermissionSet(false, false, false, false),
                accountPermissions: new PermissionSet(false, true, true, false),
                previousBalance: 100,
                newBalance: 50);

            // Able to receive funds
            await TestAccountChange(
                issuancePermissions: new PermissionSet(false, false, false, false),
                accountPermissions: new PermissionSet(false, false, true, false),
                previousBalance: 50,
                newBalance: 100);

            // Missing the affect balance permission
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                issuancePermissions: new PermissionSet(true, true, true, true),
                accountPermissions: new PermissionSet(true, true, false, true),
                previousBalance: 100,
                newBalance: 150));

            // Missing the permissions to spend from the account
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                issuancePermissions: new PermissionSet(false, false, false, false),
                accountPermissions: new PermissionSet(true, false, true, true),
                previousBalance: 150,
                newBalance: 100));

            // Not able to spend more that the funds on the account
            await Assert.ThrowsAsync<TransactionInvalidException>(() => TestAccountChange(
                issuancePermissions: new PermissionSet(false, false, false, false),
                accountPermissions: new PermissionSet(true, true, true, true),
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
                    ["/a"] = new PermissionSet(false, false, false, true)
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>();

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<LedgerPath, BinaryData>(LedgerPath.Parse("/a"), BinaryData.Parse("aabb")) });

            await validator.Validate(mutation, new SignatureEvidence[0], accounts);
        }

        [Fact]
        public async Task Validate_DataMutationError()
        {
            OpenLoopValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/a"] = new PermissionSet(true, true, true, false)
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>();

            ParsedMutation mutation = new ParsedMutation(
                new AccountStatus[0],
                new[] { new KeyValuePair<LedgerPath, BinaryData>(LedgerPath.Parse("/a"), BinaryData.Parse("aabb")) });

            await Assert.ThrowsAsync<TransactionInvalidException>(() => validator.Validate(mutation, new SignatureEvidence[0], accounts));
        }

        private static async Task TestAccountChange(PermissionSet issuancePermissions, PermissionSet accountPermissions, long previousBalance, long newBalance)
        {
            OpenLoopValidator validator = CreateValidator(
                new string[0],
                new Dictionary<string, PermissionSet>()
                {
                    ["/a"] = accountPermissions,
                    ["/b"] = issuancePermissions
                });

            Dictionary<AccountKey, AccountStatus> accounts = new Dictionary<AccountKey, AccountStatus>()
            {
                [AccountKey.Parse("/a", "/b")] = new AccountStatus(AccountKey.Parse("/a", "/b"), previousBalance, BinaryData.Empty)
            };

            ParsedMutation mutation = new ParsedMutation(
                new[] { new AccountStatus(AccountKey.Parse("/a", "/b"), newBalance, BinaryData.Empty) },
                new KeyValuePair<LedgerPath, BinaryData>[0]);

            await validator.Validate(mutation, new SignatureEvidence[0], accounts);
        }

        private static OpenLoopValidator CreateValidator(IList<string> identities, Dictionary<string, PermissionSet> getPermissions)
        {
            TestPermissionsProvider permissions = new TestPermissionsProvider(identities, getPermissions);
            return new OpenLoopValidator(permissions);
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

            public Task<PermissionSet> GetPermissions(IReadOnlyList<SignatureEvidence> identities, LedgerPath path)
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
