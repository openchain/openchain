using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OpenChain.Core;

namespace OpenChain.Ledger
{
    public class BasicValidator : IRulesValidator
    {
        private readonly HashSet<string> adminAddresses;
        private readonly ITransactionStore store;

        public BasicValidator(ITransactionStore store, string[] adminAddresses)
        {
            this.store = store;
            this.adminAddresses = new HashSet<string>(adminAddresses, StringComparer.Ordinal);
        }

        public Task ValidateAccountMutations(IReadOnlyList<AccountStatus> accountMutations, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts)
        {
            HashSet<string> signedAddresses = new HashSet<string>(authentication.Select(evidence => GetPubKeyHash(evidence.PublicKey)), StringComparer.Ordinal);

            List<AccountStatus> signedMutations = new List<AccountStatus>();
            List<AccountStatus> unsignedMutations = new List<AccountStatus>();
            foreach (AccountStatus mutation in accountMutations)
            {
                // The account must be of the form /p2pkh/{address}
                if (mutation.AccountKey.Account.IsDirectory
                    || mutation.AccountKey.Account.Segments.Count != 2
                    || mutation.AccountKey.Account.Segments[0] != "p2pkh")
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
                if (mutation.AccountKey.Asset.IsDirectory
                    || mutation.AccountKey.Asset.Segments.Count != 2
                    || mutation.AccountKey.Asset.Segments[0] != "root")
                    throw new TransactionInvalidException("InvalidAsset");

                if (signedAddresses.Contains(mutation.AccountKey.Account.Segments[1]))
                    signedMutations.Add(mutation);
                else
                    unsignedMutations.Add(mutation);
            }

            // Balance verifications if the admin is not a signer
            if (!signedAddresses.Any(address => this.adminAddresses.Contains(address)))
            {
                foreach (AccountStatus mutation in signedMutations)
                {
                    if (mutation.Balance < 0)
                        throw new TransactionInvalidException("NegativeBalance");
                }

                foreach (AccountStatus account in unsignedMutations)
                {
                    AccountStatus previousStatus = accounts[account.AccountKey];

                    if (account.Balance < previousStatus.Balance)
                        throw new TransactionInvalidException("SignatureMissing");
                }
            }

            return Task.FromResult(0);
        }

        public Task ValidateAssetDefinitionMutations(IReadOnlyList<KeyValuePair<LedgerPath, string>> assetDefinitionMutations, IReadOnlyList<SignatureEvidence> authentication)
        {
            if (assetDefinitionMutations.Count == 0)
                return Task.FromResult(0);

            if (!authentication.Any(evidence => this.adminAddresses.Contains(GetPubKeyHash(evidence.PublicKey))))
                throw new TransactionInvalidException("AdminOnlyOperation");

            foreach (KeyValuePair<LedgerPath, string> mutation in assetDefinitionMutations)
            {
                // The asset must be of the form /root/{name}
                if (mutation.Key.IsDirectory
                    || mutation.Key.Segments.Count != 2
                    || mutation.Key.Segments[0] != "root")
                    throw new TransactionInvalidException("InvalidAsset");
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
