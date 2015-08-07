using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Sqlite
{
    public class PollingObservable : IObservable<ByteString>
    {
        private readonly Func<ByteString, Task<IReadOnlyList<ByteString>>> query;
        private readonly ByteString from;

        public PollingObservable(ByteString from, Func<ByteString, Task<IReadOnlyList<ByteString>>> query)
        {
            this.from = from;
            this.query = query;
        }

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
