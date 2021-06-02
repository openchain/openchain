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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Openchain.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Openchain.Server.Models
{
    public class LedgerAnchorWorker
    {
        private readonly IServiceProvider services;

        public LedgerAnchorWorker(IServiceProvider services)
        {
            this.services = services;
        }

        public async Task Run(CancellationToken cancel)
        {
            IServiceScopeFactory scopeFactory = services.GetService<IServiceScopeFactory>();
            ILogger logger = services.GetRequiredService<ILogger>();

            while (!cancel.IsCancellationRequested)
            {
                using (IServiceScope scope = scopeFactory.CreateScope())
                {
                    IAnchorRecorder anchorRecorder = scope.ServiceProvider.GetService<IAnchorRecorder>();
                    IAnchorState anchorState = scope.ServiceProvider.GetService<IAnchorState>();

                    if (anchorRecorder == null || anchorState == null)
                    {
                        logger.LogInformation("Anchoring disabled");
                        return;
                    }

                    IStorageEngine storageEngine = scope.ServiceProvider.GetRequiredService<IStorageEngine>();

                    try
                    {
                        await storageEngine.Initialize();
                        await anchorState.Initialize();

                        AnchorBuilder anchorBuilder = new AnchorBuilder(storageEngine, anchorRecorder, anchorState);

                        while (!cancel.IsCancellationRequested)
                        {
                            LedgerAnchor anchor = await anchorBuilder.RecordAnchor();

                            if (anchor != null)
                                logger.LogInformation($"Recorded an anchor for {anchor.TransactionCount} transactions: {anchor.FullStoreHash.ToString()}");

                            await Task.Delay(TimeSpan.FromSeconds(10), cancel);
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogError($"Error in the anchor worker:\r\n{exception}");

                        // Wait longer if an error occurred
                        await Task.Delay(TimeSpan.FromMinutes(1), cancel);
                    }
                }
            }
        }
    }
}
