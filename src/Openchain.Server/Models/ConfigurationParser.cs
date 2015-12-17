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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Openchain.Ledger;
using Openchain.Ledger.Validation;

namespace Openchain.Server.Models
{
    public static class ConfigurationParser
    {
        public static ILogger CreateLogger(IServiceProvider serviceProvider)
        {
            return new DateLogger(serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("General"));
        }

        public static Task<Func<IServiceProvider, IStorageEngine>> CreateStorageEngine(IServiceProvider serviceProvider)
        {
            return DependencyResolver<IStorageEngine>.Create(serviceProvider, "storage");
        }

        public static ILedgerQueries CreateLedgerQueries(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IStorageEngine>() as ILedgerQueries;
        }

        public static ILedgerIndexes CreateLedgerIndexes(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IStorageEngine>() as ILedgerIndexes;
        }

        public static Task<Func<IServiceProvider, IAnchorState>> CreateAnchorState(IServiceProvider serviceProvider)
        {
            return DependencyResolver<IAnchorState>.Create(serviceProvider, "anchoring:storage");
        }

        public static Task<Func<IServiceProvider, IAnchorRecorder>> CreateAnchorRecorder(IServiceProvider serviceProvider)
        {
            return DependencyResolver<IAnchorRecorder>.Create(serviceProvider, "anchoring");
        }

        public static LedgerAnchorWorker CreateLedgerAnchorWorker(IServiceProvider serviceProvider)
        {
            return new LedgerAnchorWorker(serviceProvider);
        }

        public static Task<Func<IServiceProvider, IMutationValidator>> CreateRulesValidator(IServiceProvider serviceProvider)
        {
            return DependencyResolver<IMutationValidator>.Create(serviceProvider, "validator_mode:validator");
        }

        public static TransactionValidator CreateTransactionValidator(IServiceProvider serviceProvider)
        {
            IMutationValidator rulesValidator = serviceProvider.GetService<IMutationValidator>();

            if (rulesValidator == null)
            {
                return null;
            }
            else
            {
                string rootUrl = serviceProvider.GetService<IConfiguration>()["validator_mode:root_url"];

                if (!Uri.IsWellFormedUriString(rootUrl, UriKind.Absolute))
                {
                    string errorMessage = $"The server root URL is not a valid URL: '{rootUrl}'. Please make sure it is configured correctly.";
                    throw new InvalidOperationException(errorMessage);
                }

                return new TransactionValidator(serviceProvider.GetService<IStorageEngine>(), rulesValidator, rootUrl);
            }
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

                if (string.IsNullOrEmpty(upstreamUrl))
                    throw new InvalidOperationException("Observer mode is enabled but no upstream URL has been specified.");

                logger.LogInformation("Current mode: Observer mode");
                logger.LogInformation("Upstream URL: {0}", upstreamUrl);

                return new TransactionStreamSubscriber(new Uri(upstreamUrl), serviceProvider);
            }
        }
    }
}
