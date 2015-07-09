using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using OpenChain.Core;
using OpenChain.Core.Sqlite;
using OpenChain.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public static class ConfigurationParser
    {
        public static ILedgerStore CreateLedgerStore(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));

            return new SqliteTransactionStore(configuration.GetSubKey("SQLite").Get("path"));
        }

        public static ILedgerQueries CreateLedgerQueries(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            ILedgerStore store = (ILedgerStore)serviceProvider.GetService(typeof(ILedgerStore));

            return store as ILedgerQueries;
        }

        public static IRulesValidator CreateRulesValidator(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            
            if (!configuration.GetSubKey("Main").Get<bool>("is_master"))
                return ActivatorUtilities.CreateInstance<NullValidator>(serviceProvider, false);

            switch (configuration.GetSubKey("Main").Get("validator"))
            {
                case "Basic":
                    return ActivatorUtilities.CreateInstance<BasicValidator>(serviceProvider);
                case "Disabled":
                    return ActivatorUtilities.CreateInstance<NullValidator>(serviceProvider, true);
                default:
                    return null;
            }
        }

        public static ILogger CreateLogger(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));
            ILoggerFactory loggerFactory = (ILoggerFactory)serviceProvider.GetService(typeof(ILoggerFactory));
            
            return loggerFactory.CreateLogger("General");
        }

        public static IStreamSubscriber CreateStreamSubscriber(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = (IConfiguration)serviceProvider.GetService(typeof(IConfiguration));

            string masterUrl = configuration.GetSubKey("Main").Get("master_url");
            if (!string.IsNullOrEmpty(masterUrl) && !configuration.GetSubKey("Main").Get<bool>("is_master"))
            {
                TransactionStreamSubscriber streamSubscriber = ActivatorUtilities.CreateInstance<TransactionStreamSubscriber>(serviceProvider, new Uri(masterUrl));
                streamSubscriber.Subscribe(CancellationToken.None);

                return streamSubscriber;
            }
            else
            {
                return null;
            }
        }
    }
}
