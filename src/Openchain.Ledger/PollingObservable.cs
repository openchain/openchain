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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Openchain.Ledger
{
    /// <summary>
    /// Represents an observable collection constructed through polling.
    /// </summary>
    public class PollingObservable : IObservable<ByteString>
    {
        private readonly Func<ByteString, Task<IReadOnlyList<ByteString>>> query;
        private readonly ByteString from;

        public PollingObservable(ByteString from, Func<ByteString, Task<IReadOnlyList<ByteString>>> query)
        {
            this.from = from;
            this.query = query;
        }

        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        /// <returns>A reference to an interface that allows observers to stop receiving notifications
        /// before the provider has finished sending them.</returns>
        public IDisposable Subscribe(IObserver<ByteString> observer)
        {
            Subscription subscription = new Subscription(this, observer);
            subscription.Start(observer);
            return subscription;
        }

        private class Subscription : IDisposable
        {
            private readonly PollingObservable parent;
            private readonly CancellationTokenSource cancel = new CancellationTokenSource();

            public Subscription(PollingObservable parent, IObserver<ByteString> observer)
            {
                this.parent = parent;
            }

            public async void Start(IObserver<ByteString> observer)
            {
                try
                {
                    ByteString cursor = parent.from;

                    while (!cancel.Token.IsCancellationRequested)
                    {
                        IReadOnlyList<ByteString> result = await parent.query(cursor);

                        ByteString lastRecord = null;
                        foreach (ByteString record in result)
                        {
                            observer.OnNext(record);
                            lastRecord = record;
                        }

                        if (lastRecord != null)
                            cursor = new ByteString(MessageSerializer.ComputeHash(lastRecord.ToByteArray()));

                        await Task.Delay(TimeSpan.FromSeconds(0.2));
                    }

                    observer.OnCompleted();
                }
                catch (Exception exception)
                {
                    observer.OnError(exception);
                }
            }

            public void Dispose()
            {
                cancel.Cancel();
            }
        }
    }
}
