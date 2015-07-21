using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using OpenChain.Core;
using OpenChain.Ledger;
using OpenChain.Sqlite;
using System;
using System.Threading;

namespace OpenChain.Models
{
    public static class ConfigurationParser
    {
        public static ITransactionStore CreateLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();

            return new SqliteLedgerQueries(configuration.GetConfigurationSection("SQLite").Get("path"));
        }

        public static ILedgerQueries CreateLedgerQueries(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ITransactionStore store = serviceProvider.GetService<ITransactionStore>();

            return store as ILedgerQueries;
        }

        public static IRulesValidator CreateRulesValidator(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ILogger logger = serviceProvider.GetService<ILogger>();

            if (!bool.Parse(configuration.GetConfigurationSection("Main").Get("is_master")))
            {
                logger.LogInformation("Transaction validation mode disabled (Slave mode)");
                return ActivatorUtilities.CreateInstance<NullValidator>(serviceProvider, false);
            }
            else
            {
                logger.LogInformation("Transaction validation mode enabled (Master mode)");
            }

            switch (configuration.GetConfigurationSection("Main").Get("validator"))
            {
                case "Basic":
                    string[] adminAddresses = configuration.GetConfigurationSection("BasicValidator").Get("admin_addresses").Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    return new BasicValidator(serviceProvider.GetRequiredService<ITransactionStore>(), adminAddresses);
                case "Disabled":
                    return ActivatorUtilities.CreateInstance<NullValidator>(serviceProvider, true);
                default:
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
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
            ILogger logger = serviceProvider.GetService<ILogger>();

            string masterUrl = configuration.GetConfigurationSection("Main").Get("master_url");
            if (!string.IsNullOrEmpty(masterUrl) && !bool.Parse(configuration.GetConfigurationSection("Main").Get("is_master")))
            {
                logger.LogInformation("Stream subscriber enabled, master URL: {0}", masterUrl);
                TransactionStreamSubscriber streamSubscriber = ActivatorUtilities.CreateInstance<TransactionStreamSubscriber>(serviceProvider, new Uri(masterUrl));
                streamSubscriber.Subscribe(CancellationToken.None);

                return streamSubscriber;
            }
            else
            {
                logger.LogInformation("Stream subscriber disabled");
                return null;
            }
        }
    }
}
