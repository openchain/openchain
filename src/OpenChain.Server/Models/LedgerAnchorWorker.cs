using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using OpenChain.Ledger;

namespace OpenChain.Server.Models
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
                    LedgerAnchor anchor = await this.anchorBuilder.CreateAnchor();

                    await this.anchorRecorder.RecordAnchor(anchor);
                }
                catch (Exception exception)
                {
                    logger.LogError($"Error in the anchor worker:\r\n{exception}");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancel);
            }
        }
    }
}
