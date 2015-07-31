using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using OpenChain.Ledger;
using OpenChain.Sqlite;

namespace OpenChain.Server.Models
{
    public static class ConfigurationParser
    {
        public static ITransactionStore CreateLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration storage = configuration.GetConfigurationSection("storage");
            if (storage["type"] == "SQLite")
                return new SqliteLedgerQueries(storage["path"]);
            else
                throw new NotSupportedException();
        }

        public static ILedgerQueries CreateLedgerQueries(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ITransactionStore store = serviceProvider.GetService<ITransactionStore>();

            return store as ILedgerQueries;
        }

        public static async Task InitializeLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            IConfiguration storage = configuration.GetConfigurationSection("storage");

            SqliteLedgerQueries store = new SqliteLedgerQueries(storage["path"]);

            if (storage["type"] == "SQLite")
                await store.EnsureTables();
        }

        public static IMutationValidator CreateRulesValidator(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>().GetConfigurationSection("master_mode");
            ILogger logger = serviceProvider.GetService<ILogger>();

            if (configuration["root_url"] != null)
            {
                logger.LogInformation("Transaction validation mode enabled (Master mode)");
                IConfiguration validator = configuration.GetConfigurationSection("validator");

                switch (validator["type"])
                {
                    case "OpenLoop":
                        string[] adminAddresses = validator.GetConfigurationSections("admin_addresses").Select(key => validator.GetConfigurationSection("admin_addresses").Get(key.Key)).ToArray();
                        List<PathPermissions> pathPermissions = new List<PathPermissions>();
                        pathPermissions.Add(new PathPermissions(LedgerPath.Parse("/"), new PermissionSet(true, true, true, true), adminAddresses));
                        
                        foreach (KeyValuePair<string, IConfiguration> pair in validator.GetConfigurationSections("issuers"))
                        {
                            string[] addresses = pair.Value.GetConfigurationSections("addresses").Select(key => pair.Value.GetConfigurationSection("addresses").Get(key.Key)).ToArray();

                            pathPermissions.Add(new PathPermissions(
                                LedgerPath.Parse(pair.Value.Get("path")),
                                new PermissionSet(true, true, true, true),
                                addresses));
                        }

                        bool allowThirdPartyAssets = bool.Parse(validator["allow_third_party_assets"]);
                        byte versionByte = byte.Parse(validator["version_byte"]);
                        IPermissionsProvider permissions = new DefaultPermissionLayout(pathPermissions, allowThirdPartyAssets, versionByte);
                        return new OpenLoopValidator(permissions);
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
                return new TransactionValidator(serviceProvider.GetService<ITransactionStore>(), rulesValidator, serviceProvider.GetService<IConfiguration>().Get("master_mode:root_url"));
        }

        public static MasterProperties CreateMasterProperties(IServiceProvider serviceProvider)
        {
            IMutationValidator master = serviceProvider.GetService<IMutationValidator>();

            if (master != null)
            {
                IConfiguration configuration = serviceProvider.GetService<IConfiguration>().GetConfigurationSection("master_mode").GetConfigurationSection("properties");
                return new MasterProperties(configuration);
            }
            else
            {
                return null;
            }
        }

        public static ILogger CreateLogger(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            return loggerFactory.CreateLogger("General");
        }

        public static IStreamSubscriber CreateStreamSubscriber(IServiceProvider serviceProvider)
        {
            ILogger logger = serviceProvider.GetService<ILogger>();

            if (serviceProvider.GetService<IMutationValidator>() != null)
            {
                logger.LogInformation("Stream subscriber disabled");
                return null;
            }
            else
            {
                IConfiguration observerMode = serviceProvider.GetService<IConfiguration>().GetConfigurationSection("observer_mode");

                string masterUrl = observerMode["master_url"];
                logger.LogInformation("Stream subscriber enabled, master URL: {0}", masterUrl);
                TransactionStreamSubscriber streamSubscriber = ActivatorUtilities.CreateInstance<TransactionStreamSubscriber>(serviceProvider, new Uri(masterUrl));
                streamSubscriber.Subscribe(CancellationToken.None);

                return streamSubscriber;
            }
        }
    }
}
