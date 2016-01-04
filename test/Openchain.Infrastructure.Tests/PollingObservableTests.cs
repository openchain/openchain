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
using System.Threading.Tasks;
using Xunit;

namespace Openchain.Infrastructure.Tests
{
    public class PollingObservableTests
    {
        [Fact]
        public async Task Subscribe_Success()
        {
            TestObserver observer = new TestObserver() { ExpectedValueCount = 4 };

            int current = 0;
            Func<ByteString, Task<IReadOnlyList<ByteString>>> query = delegate (ByteString start)
            {
                List<ByteString> result = new List<ByteString>();
                result.Add(new ByteString(new byte[] { (byte)current, 0 }));
                result.Add(new ByteString(new byte[] { (byte)current, 1 }));
                current++;

                return Task.FromResult<IReadOnlyList<ByteString>>(result);
            };

            IObservable<ByteString> stream = new PollingObservable(ByteString.Empty, query);
            using (stream.Subscribe(observer))
                await observer.Completed.Task;

            await observer.Disposed.Task;

            Assert.False(observer.Fail);
            Assert.Equal(4, observer.Values.Count);
            Assert.Equal(ByteString.Parse("0000"), observer.Values[0]);
            Assert.Equal(ByteString.Parse("0001"), observer.Values[1]);
            Assert.Equal(ByteString.Parse("0100"), observer.Values[2]);
            Assert.Equal(ByteString.Parse("0101"), observer.Values[3]);
        }

        static Task<T> FromExAsync<T>(Exception ex)
        {
            var ei = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
            var task = new Task<T>(() => { ei.Throw(); return default(T); });
            task.RunSynchronously();
            return task;
        }

        [Fact]
        public async Task Subscribe_Error()
        {
            TestObserver observer = new TestObserver() { ExpectedValueCount = 1 };

            Func<ByteString, Task<IReadOnlyList<ByteString>>> query = delegate (ByteString start)
            {
                return FromExAsync<IReadOnlyList<ByteString>>(new ArithmeticException());
            };

            IObservable<ByteString> stream = new PollingObservable(ByteString.Empty, query);
            using (stream.Subscribe(observer))
                await observer.Completed.Task;

            await observer.Disposed.Task;

            Assert.True(observer.Fail);
            Assert.Equal(0, observer.Values.Count);
        }

        private class TestObserver : IObserver<ByteString>
        {
            public int ExpectedValueCount { get; set; }

            public TaskCompletionSource<int> Completed { get; } = new TaskCompletionSource<int>();

            public TaskCompletionSource<int> Disposed { get; } = new TaskCompletionSource<int>();

            public IList<ByteString> Values { get; } = new List<ByteString>();

            public bool Fail { get; set; }

            public void OnCompleted() => Disposed.SetResult(0);

            public void OnError(Exception error)
            {
                Fail = true;
                this.Completed.SetResult(0);
                Disposed.SetResult(0);
            }

            public void OnNext(ByteString value)
            {
                Values.Add(value);

                if (Values.Count == ExpectedValueCount)
                    this.Completed.SetResult(0);
            }
        }
    }
}
