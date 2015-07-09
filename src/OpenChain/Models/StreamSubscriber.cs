using Microsoft.AspNet.WebSockets.Client;
using Microsoft.Framework.Logging;
using Newtonsoft.Json.Linq;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class TransactionStreamSubscriber : IStreamSubscriber
    {
        private readonly Uri endpoint;
        private readonly ILedgerStore store;
        private readonly ILogger logger;

        public TransactionStreamSubscriber(Uri endpoint, ILedgerStore store, ILogger logger)
        {
            this.endpoint = new Uri(endpoint, "stream");
            this.store = store;
            this.logger = logger;
        }

        public async Task Subscribe(CancellationToken cancel)
        {
            byte[] buffer = new byte[1024 * 1024];
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    ClientWebSocket socket = new ClientWebSocket();

                    logger.LogInformation("Connecting to {0}", endpoint);
                    //WebSocket socket = await wsClient.ConnectAsync(this.endpoint, cancel);
                    await socket.ConnectAsync(this.endpoint, cancel);

                    while (true)
                    {
                        WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancel);
                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        await store.AddLedgerRecord(new BinaryData(buffer.Take(result.Count)));
                    }
                    //HttpClient client = new HttpClient();
                    //string response = await client.GetStringAsync(this.endpoint);

                    //JArray records = JArray.Parse(response);

                    //foreach (JObject record in records)
                    //{
                    //    BinaryData rawRecord = BinaryData.Parse((string)record["raw"]);
                    //    await this.store.AddLedgerRecord(rawRecord);
                    //}
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
