using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenChain.Core;

namespace OpenChain.Ledger
{
    public class OpenLoopValidator : IRulesValidator
    {
        private readonly HashSet<string> adminAddresses;
        private readonly ITransactionStore store;
        private readonly bool allowThirdPartyAssets;
        private readonly LedgerPath adminAssetRoot = LedgerPath.Parse("/root");
        private readonly LedgerPath p2pkhAssetRoot = LedgerPath.Parse("/p2pkh");
        private readonly LedgerPath p2pkhAccountRoot = LedgerPath.Parse("/p2pkh");

        public OpenLoopValidator(ITransactionStore store, string[] adminAddresses, bool allowThirdPartyAssets)
        {
            this.store = store;
            this.adminAddresses = new HashSet<string>(adminAddresses, StringComparer.Ordinal);
            this.allowThirdPartyAssets = allowThirdPartyAssets;
        }

        public Task ValidateAccountMutations(IReadOnlyList<AccountStatus> accountMutations, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
        {
            HashSet<string> signedAddresses = new HashSet<string>(authentication.Select(evidence => GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);

            List<AccountStatus> signedMutations = new List<AccountStatus>();
            List<AccountStatus> unsignedMutations = new List<AccountStatus>();
            bool adminSigned = signedAddresses.Any(address => this.adminAddresses.Contains(address));

            foreach (AccountStatus mutation in accountMutations)
            {
                // The account must be of the form /p2pkh/{address}
                if (mutation.AccountKey.Account.IsDirectory || !p2pkhAccountRoot.IsStrictParentOf(mutation.AccountKey.Account))
                    throw new TransactionInvalidException("InvalidAccount");

                // The address must be a valid base 58 address with version byte set to 0
                try
                {
                    byte[] pubKeyHash = Base58CheckEncoding.Decode(mutation.AccountKey.Account.Segments[1]);
                    if (pubKeyHash.Length != 21 || pubKeyHash[0] != 0)
                        throw new TransactionInvalidException("InvalidAccount");
                }
                catch (FormatException)
                {
                    throw new TransactionInvalidException("InvalidAccount");
                }

                // The asset must be of the form /root/{name}
                if (mutation.AccountKey.Asset.IsDirectory)
                    throw new TransactionInvalidException("InvalidAsset");

                if (!adminAssetRoot.IsStrictParentOf(mutation.AccountKey.Asset) && !p2pkhAssetRoot.IsStrictParentOf(mutation.AccountKey.Asset))
                    throw new TransactionInvalidException("InvalidAsset");

                if (adminSigned || signedAddresses.Contains(mutation.AccountKey.Account.Segments[1]))
                    signedMutations.Add(mutation);
                else
                    unsignedMutations.Add(mutation);
            }

            foreach (AccountStatus mutation in signedMutations)
            {
                // Issuing an asset
                if (mutation.Balance < mutation.Balance && mutation.Balance < 0)
                {
                    if (this.allowThirdPartyAssets && p2pkhAssetRoot.IsStrictParentOf(mutation.AccountKey.Asset))
                    {
                        if (mutation.AccountKey.Account.Segments[1] != mutation.AccountKey.Asset.Segments[1])
                            throw new TransactionInvalidException("NegativeBalance");

                    }
                    else if (adminAssetRoot.IsStrictParentOf(mutation.AccountKey.Asset))
                    {
                        if (!adminSigned)
                            throw new TransactionInvalidException("NegativeBalance");
                    }

                    throw new TransactionInvalidException("NegativeBalance");
                }
            }

            foreach (AccountStatus account in unsignedMutations)
            {
                AccountStatus previousStatus = accounts[account.AccountKey];

                if (account.Balance < previousStatus.Balance)
                    throw new TransactionInvalidException("SignatureMissing");
            }

            return Task.FromResult(0);
        }

        public Task ValidateAssetDefinitionMutations(IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations, IReadOnlyList<SignatureEvidence> authentication)
        {
            if (assetDefinitionMutations.Count == 0)
                return Task.FromResult(0);

            HashSet<string> signedAddresses = new HashSet<string>(authentication.Select(evidence => GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);
            bool adminSigned = signedAddresses.Any(address => this.adminAddresses.Contains(address));

            foreach (KeyValuePair<LedgerPath, string> mutation in assetDefinitionMutations)
            {
                if (this.allowThirdPartyAssets && p2pkhAssetRoot.IsStrictParentOf(mutation.Key))
                {
                    if (signedAddresses.Contains(mutation.Key.Segments[1]))
                        continue;
                }
                else if (adminAssetRoot.IsStrictParentOf(mutation.Key))
                {
                    if (adminSigned)
                        continue;
                }

                throw new TransactionInvalidException("InvalidSignature");
            }

            return Task.FromResult(0);
        }

        private static string GetPubKeyHash(BinaryData pubKey)
        {
            using (RIPEMD160 ripe = RIPEMD160.Create())
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] result = ripe.ComputeHash(sha256.ComputeHash(pubKey.ToByteArray()));
                return Base58CheckEncoding.Encode(new byte[] { 0 }.Concat(result).ToArray());
            }
        }
    }
}
