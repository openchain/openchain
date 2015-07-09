using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using OpenChain.Core;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class TransactionStreamMiddleware
    {
        private readonly RequestDelegate next;

        public TransactionStreamMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.IsWebSocketRequest)
            {
                string from = context.Request.Query.Get("from");
                BinaryData lastLedgerRecordHash;
                if (string.IsNullOrEmpty(from))
                    lastLedgerRecordHash = null;
                else
                    lastLedgerRecordHash = BinaryData.Parse(from);

                ILedgerStore store = (ILedgerStore)context.ApplicationServices.GetService(typeof(ILedgerStore));

                IObservable<BinaryData> stream = store.GetRecordStream(lastLedgerRecordHash);

                using (WebSocket webSocket = await context.AcceptWebSocketAsync())
                {
                    ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(new byte[512]);

                    using (Observer observer = new Observer(webSocket, context.RequestAborted))
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

        private class Observer : IObserver<BinaryData>, IDisposable
        {
            private readonly WebSocket webSocket;
            private Task currentTask = Task.FromResult(0);
            private readonly object currentTaskLock = new object();
            private readonly TaskCompletionSource<int> completed = new TaskCompletionSource<int>();
            private readonly CancellationToken cancel;
            private readonly CancellationTokenRegistration registration;

            public Observer(WebSocket webSocket, CancellationToken cancel)
            {
                this.webSocket = webSocket;
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
                QueueTask(async () =>
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.InternalServerError,
                        "Internal Server Error",
                        cancel);

                    completed.TrySetResult(0);
                });
            }

            public void OnNext(BinaryData value)
            {
                QueueTask(() => webSocket.SendAsync(
                    new ArraySegment<byte>(value.ToArray()),
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
