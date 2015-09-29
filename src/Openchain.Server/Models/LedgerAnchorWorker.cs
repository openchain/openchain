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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using Openchain.Ledger;

namespace Openchain.Server.Models
{
    public class LedgerAnchorWorker
    {
        private readonly IAnchorBuilder anchorBuilder;
        private readonly IAnchorRecorder anchorRecorder;
        private readonly ILogger logger;

        public LedgerAnchorWorker(IAnchorBuilder anchorBuilder, IAnchorRecorder anchorRecorder, ILogger logger)
        {
            this.anchorBuilder = anchorBuilder;
            this.anchorRecorder = anchorRecorder;
            this.logger = logger;
        }

        public async Task Run(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    if (await this.anchorRecorder.CanRecordAnchor())
                    {
                        LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor();

                        if (anchor != null)
                        {
                            logger.LogInformation($"Recording anchor for {anchor.TransactionCount} transaction(s)");

                            // Record the anchor
                            await this.anchorRecorder.RecordAnchor(anchor);

                            // Commit the anchor if it has been recorded successfully
                            await this.anchorBuilder.CommitAnchor(anchor);
                        }
                    }

                }
                catch (Exception exception)
                {
                    logger.LogError($"Error in the anchor worker:\r\n{exception}");

                    // Wait longer if an error occurred
                    await Task.Delay(TimeSpan.FromMinutes(1), cancel);
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancel);
            }
        }
    }
}
