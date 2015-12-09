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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Openchain.Ledger;
using Openchain.Ledger.Blockchain;
using Openchain.Ledger.Validation;
using Openchain.Sqlite;

namespace Openchain.Server.Models
{
    public static class ConfigurationParser
    {
        public static IStorageEngine CreateLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration storage = configuration.GetSection("storage");

            try
            {
                if (storage["type"] == "Sqlite")
                {
                    return new SqliteLedgerQueries(GetPathOrDefault(serviceProvider, storage["path"]));
                }
            }
            catch (Exception exception)
            {
                serviceProvider.GetRequiredService<ILogger>().LogError($"Error while instantiating the transaction store:\n {exception}");
                throw;
            }

            throw new NotSupportedException();
        }

        public static ILedgerQueries CreateLedgerQueries(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IStorageEngine store = serviceProvider.GetService<IStorageEngine>();

            return store as ILedgerQueries;
        }

        public static ILedgerIndexes CreateLedgerIndexes(IServiceProvider serviceProvider)
        {
            IStorageEngine store = serviceProvider.GetService<IStorageEngine>();

            return store as ILedgerIndexes;
        }

        public static IAnchorBuilder CreateAnchorBuilder(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration storage = configuration.GetSection("storage");

            if (storage["type"] == "Sqlite")
            {
                SqliteAnchorBuilder result = new SqliteAnchorBuilder(GetPathOrDefault(serviceProvider, storage["path"]));
                result.EnsureTables().Wait();
                return result;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static LedgerAnchorWorker CreateLedgerAnchorWorker(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration anchoring = configuration.GetSection("anchoring");
            ILogger logger = serviceProvider.GetService<ILogger>();

            IAnchorRecorder recorder = null;
            switch (anchoring["type"])
            {
                case "blockchain":
                    string anchorKey = anchoring["key"];
                    if (!string.IsNullOrEmpty(anchorKey))
                    {
                        NBitcoin.Key key = NBitcoin.Key.Parse(anchorKey);
                        NBitcoin.Network network = NBitcoin.Network.GetNetworks()
                            .First(item => item.GetVersionBytes(NBitcoin.Base58Type.PUBKEY_ADDRESS)[0] == byte.Parse(anchoring["network_byte"]));

                        logger.LogInformation($"Starting Blockchain anchor (address: {key.PubKey.GetAddress(network).ToString()})");
                        recorder = new BlockchainAnchorRecorder(new Uri(anchoring["bitcoin_api_url"]), key, network, long.Parse(anchoring["fees"]));
                    }
                    break;
            }

            if (recorder != null)
                return new LedgerAnchorWorker(serviceProvider.GetRequiredService<IAnchorBuilder>(), recorder, serviceProvider.GetRequiredService<ILogger>());
            else
                return null;
        }

        public static async Task InitializeLedgerStore(IServiceProvider serviceProvider)
        {
            SqliteLedgerQueries store = serviceProvider.GetService<ILedgerQueries>() as SqliteLedgerQueries;

            if (store != null)
                await store.EnsureTables();
        }

        public static IMutationValidator CreateRulesValidator(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>().GetSection("validator_mode");
            ILogger logger = serviceProvider.GetService<ILogger>();

            string rootUrl = configuration["root_url"];
            if (rootUrl != null)
            {
                if (!Uri.IsWellFormedUriString(rootUrl, UriKind.Absolute))
                {
                    string errorMessage = $"The server root URL is not a valid URL: '{rootUrl}'. Please make sure it is configured correctly.";
                    throw new InvalidOperationException(errorMessage);
                }

                logger.LogInformation("Current mode: Validator mode");
                logger.LogInformation($"Namespace: {rootUrl}");
                IConfiguration validator = configuration.GetSection("validator");

                switch (validator["type"])
                {
                    case "PermissionBased":
                        byte versionByte = byte.Parse(validator["version_byte"]);
                        KeyEncoder keyEncoder = new KeyEncoder(versionByte);

                        P2pkhSubject[] adminAddresses = validator
                            .GetSection("admin_addresses")
                            .GetChildren()
                            .Select(key => key.Value)
                            .Select(address => new P2pkhSubject(new[] { address }, 1, keyEncoder))
                            .ToArray();

                        List<Acl> pathPermissions = new List<Acl>()
                        {
                            // Admins have full rights
                            new Acl(adminAddresses, LedgerPath.Parse("/"), true, StringPattern.MatchAll, PermissionSet.AllowAll)
                        };

                        List<IPermissionsProvider> permissionProviders = new List<IPermissionsProvider>();

                        if (bool.Parse(validator["allow_third_party_assets"]))
                            permissionProviders.Add(new P2pkhIssuanceImplicitLayout(keyEncoder));

                        if (bool.Parse(validator["allow_p2pkh_accounts"]))
                            permissionProviders.Add(new P2pkhImplicitLayout(keyEncoder));

                        permissionProviders.Add(new StaticPermissionLayout(pathPermissions));
                        permissionProviders.Add(new DynamicPermissionLayout(serviceProvider.GetRequiredService<IStorageEngine>(), keyEncoder));

                        return new PermissionBasedValidator(permissionProviders);
                    case "Disabled":
                        return ActivatorUtilities.CreateInstance<NullValidator>(serviceProvider, true);
                    default:
                        return null;
                }
            }
            else
            {
                logger.LogInformation("Transaction validation mode disabled (Slave mode)");
                return null;
            }
        }

        public static TransactionValidator CreateTransactionValidator(IServiceProvider serviceProvider)
        {
            IMutationValidator rulesValidator = serviceProvider.GetService<IMutationValidator>();

            if (rulesValidator == null)
                return null;
            else
                return new TransactionValidator(serviceProvider.GetService<IStorageEngine>(), rulesValidator, serviceProvider.GetService<IConfiguration>()["validator_mode:root_url"]);
        }

        public static ILogger CreateLogger(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            return new DateLogger(loggerFactory.CreateLogger("General"));
        }

        public static TransactionStreamSubscriber CreateStreamSubscriber(IServiceProvider serviceProvider)
        {
            ILogger logger = serviceProvider.GetService<ILogger>();

            if (serviceProvider.GetService<IMutationValidator>() != null)
            {
                logger.LogInformation("Stream subscriber disabled");
                return null;
            }
            else
            {
                IConfiguration observerMode = serviceProvider.GetService<IConfiguration>().GetSection("observer_mode");

                string upstreamUrl = observerMode["upstream_url"];
                logger.LogInformation("Current mode: Observer mode");
                logger.LogInformation("Upstream URL: {0}", upstreamUrl);
                return ActivatorUtilities.CreateInstance<TransactionStreamSubscriber>(serviceProvider, new Uri(upstreamUrl));
            }
        }

        private static string GetPathOrDefault(IServiceProvider serviceProvider, string path)
        {
            IHostingEnvironment environment = serviceProvider.GetService<IHostingEnvironment>();
            return environment.MapPath(Path.Combine("App_Data", path));
        }
    }
}
