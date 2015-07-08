using Microsoft.Framework.ConfigurationModel;
using OpenChain.Core;
using OpenChain.Core.Sqlite;
using OpenChain.Server;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ILedgerQueries queries = (ILedgerQueries)serviceProvider.GetService(typeof(ILedgerQueries));

            if (queries != null)
                return new BasicValidator(queries);
            else
                return null;
        }
    }
}
