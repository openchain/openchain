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
using Microsoft.Extensions.PlatformAbstractions;
using Openchain.Ledger;
using Openchain.Ledger.Blockchain;
using Openchain.Ledger.Validation;
using Openchain.Sqlite;

namespace Openchain.Server.Models
{
    public static class ConfigurationParser
    {
        public static Func<IServiceProvider, IStorageEngine> CreateLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IAssemblyLoadContextAccessor assemblyLoader = serviceProvider.GetService<IAssemblyLoadContextAccessor>();

            try
            {
                DependencyResolver<IStorageEngine> resolver = DependencyResolver<IStorageEngine>.Create(configuration.GetSection("storage"), assemblyLoader);
                return _ => resolver.Build();
            }
            catch (Exception exception)
            {
                serviceProvider.GetRequiredService<ILogger>().LogError($"Error while instantiating the transaction store:\n {exception}");
                throw;
            }
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

        public static Func<IServiceProvider, IAnchorBuilder> CreateAnchorBuilder(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration recording = configuration.GetSection("anchoring").GetSection("recording");
            IAssemblyLoadContextAccessor assemblyLoader = serviceProvider.GetService<IAssemblyLoadContextAccessor>();

            try
            {
                DependencyResolver<IAnchorBuilder> resolver = DependencyResolver<IAnchorBuilder>.Create(recording, assemblyLoader);
                return _ => resolver.Build();
            }
            catch (Exception exception)
            {
                serviceProvider.GetRequiredService<ILogger>().LogError($"Error while instantiating the anchor builder:\n {exception}");
                throw;
            }
        }

        public static IAnchorRecorder CreateAnchorRecorder(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration anchoring = configuration.GetSection("anchoring");
            ILogger logger = serviceProvider.GetService<ILogger>();

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
                        return new BlockchainAnchorRecorder(new Uri(anchoring["bitcoin_api_url"]), key, network, long.Parse(anchoring["fees"]));
                    }
                    break;
            }

            return null;
        }

        public static LedgerAnchorWorker CreateLedgerAnchorWorker(IServiceProvider serviceProvider)
        {
            return new LedgerAnchorWorker(serviceProvider);
        }

        public static async Task InitializeLedgerStore(IServiceProvider serviceProvider)
        {
            SqliteLedger store = serviceProvider.GetService<ILedgerQueries>() as SqliteLedger;

            if (store != null)
                await store.Initialize();
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
