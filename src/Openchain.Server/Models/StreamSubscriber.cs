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

using Microsoft.Framework.Logging;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Openchain.Server.Models
{
    public class TransactionStreamSubscriber
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

            ByteString currentRecord = await this.store.GetLastTransaction();

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

                        ByteString record = new ByteString(buffer.Take(result.Count));
                        await store.AddTransactions(new[] { record });

                        currentRecord = new ByteString(MessageSerializer.ComputeHash(record.ToByteArray()));
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
