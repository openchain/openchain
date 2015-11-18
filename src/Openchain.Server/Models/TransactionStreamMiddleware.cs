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
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace Openchain.Server.Models
{
    /// <summary>
    /// Represents the middleware used to expose the transaction stream.
    /// </summary>
    public class TransactionStreamMiddleware
    {
        private readonly RequestDelegate next;

        public TransactionStreamMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Handles an incoming HTTP request.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                string from = context.Request.Query["from"];
                ByteString lastLedgerRecordHash;
                if (string.IsNullOrEmpty(from))
                    lastLedgerRecordHash = null;
                else
                    lastLedgerRecordHash = ByteString.Parse(from);

                ILogger logger = (ILogger)context.ApplicationServices.GetService(typeof(ILogger));
                IStorageEngine store = (IStorageEngine)context.ApplicationServices.GetService(typeof(IStorageEngine));

                IObservable<ByteString> stream = store.GetTransactionStream(lastLedgerRecordHash);

                using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
                {
                    ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512]);

                    using (Observer observer = new Observer(webSocket, logger, context.RequestAborted))
                    {
                        using (stream.Subscribe(observer))
                        {
                            await Task.WhenAny(observer.Closed, webSocket.ReceiveAsync(receiveBuffer, context.RequestAborted));
                        }

                        await observer.Closed;
                    }
                }
            }
            else
            {
                await this.next(context);
            }
        }

        private class Observer : IObserver<ByteString>, IDisposable
        {
            private readonly WebSocket webSocket;
            private readonly ILogger logger;
            private Task currentTask = Task.FromResult(0);
            private readonly object currentTaskLock = new object();
            private readonly TaskCompletionSource<int> completed = new TaskCompletionSource<int>();
            private readonly CancellationToken cancel;
            private readonly CancellationTokenRegistration registration;

            public Observer(WebSocket webSocket, ILogger logger, CancellationToken cancel)
            {
                this.webSocket = webSocket;
                this.logger = logger;
                this.cancel = cancel;
                this.registration = cancel.Register(() => completed.TrySetResult(0));
            }

            public Task Closed => completed.Task;

            public void OnCompleted()
            {
                QueueTask(async () =>
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.EndpointUnavailable,
                        "Endpoint closing",
                        cancel);

                    completed.TrySetResult(0);
                });
            }

            public void OnError(Exception error)
            {
                this.logger.LogError($"An error occured in the transaction stream server: {error.ToString()}");

                QueueTask(async () =>
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Internal Server Error",
                        cancel);

                    completed.TrySetResult(0);
                });
            }

            public void OnNext(ByteString value)
            {
                QueueTask(() => webSocket.SendAsync(
                    new ArraySegment<byte>(value.ToByteArray()),
                    WebSocketMessageType.Binary,
                    true,
                    cancel));
            }

            private void QueueTask(Func<Task> operation)
            {
                lock (currentTaskLock)
                {
                    Task previousTask = this.currentTask;
                    this.currentTask = Task.Run(
                        async delegate ()
                        {
                            await previousTask;

                            if (!Closed.IsCompleted)
                            {
                                try
                                {
                                    await operation();
                                }
                                catch
                                {
                                    completed.TrySetResult(0);
                                }
                            }
                        });
                }
            }

            public void Dispose()
            {
                completed.TrySetResult(0);
                registration.Dispose();
            }
        }
    }
}
