using Microsoft.Framework.Logging;
using OpenChain.Core;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class TransactionStreamSubscriber : IStreamSubscriber
    {
        private readonly UriBuilder endpoint;
        private readonly ITransactionStore store;
        private readonly ILogger logger;

        public TransactionStreamSubscriber(Uri endpoint, ITransactionStore store, ILogger logger)
        {
            this.endpoint = new UriBuilder(endpoint);
            this.endpoint.Scheme = "ws";
            this.endpoint.Path = this.endpoint.Path.TrimEnd('/') + "/stream";
            this.store = store;
            this.logger = logger;
        }

        public async Task Subscribe(CancellationToken cancel)
        {
            byte[] buffer = new byte[1024 * 1024];
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

            BinaryData currentRecord = await this.store.GetLastTransaction();

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    ClientWebSocket socket = new ClientWebSocket();

                    this.endpoint.Query = string.Format("from={0}", currentRecord.ToString());

                    logger.LogInformation("Connecting to {0}", this.endpoint.Uri);

                    await socket.ConnectAsync(this.endpoint.Uri, cancel);

                    while (true)
                    {
                        WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancel);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        BinaryData record = new BinaryData(buffer.Take(result.Count));
                        await store.AddTransactions(new[] { record });

                        currentRecord = new BinaryData(MessageSerializer.ComputeHash(record.ToByteArray()));
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError("Error in the stream subscriber: {0}", exception.ToString());
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}
