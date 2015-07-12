using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Core.Sqlite
{
    public class PollingObservable : IObservable<BinaryData>
    {
        private readonly Func<BinaryData, Task<IReadOnlyList<BinaryData>>> query;
        private readonly BinaryData from;

        public PollingObservable(BinaryData from, Func<BinaryData, Task<IReadOnlyList<BinaryData>>> query)
        {
            this.from = from;
            this.query = query;
        }

        public IDisposable Subscribe(IObserver<BinaryData> observer)
        {
            Subscription subscription = new Subscription(this, observer);
            subscription.Start(observer);
            return subscription;
        }

        private class Subscription : IDisposable
        {
            private readonly PollingObservable parent;
            private readonly CancellationTokenSource cancel = new CancellationTokenSource();

            public Subscription(PollingObservable parent, IObserver<BinaryData> observer)
            {
                this.parent = parent;
            }

            public async void Start(IObserver<BinaryData> observer)
            {
                try
                {
                    BinaryData cursor = parent.from;

                    while (!cancel.Token.IsCancellationRequested)
                    {
                        IReadOnlyList<BinaryData> result = await parent.query(cursor);

                        BinaryData lastRecord = null;
                        foreach (BinaryData record in result)
                        {
                            observer.OnNext(record);
                            lastRecord = record;
                        }

                        if (lastRecord != null)
                            cursor = new BinaryData(MessageSerializer.ComputeHash(lastRecord));

                        await Task.Delay(TimeSpan.FromSeconds(1));
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
