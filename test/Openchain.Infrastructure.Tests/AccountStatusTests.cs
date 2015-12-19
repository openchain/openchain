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
using System.Linq;
using Assert = Xunit.Assert;
using Fact = Xunit.FactAttribute;

namespace Openchain.Infrastructure.Tests
{
    public class AccountStatusTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void AccountStatus_Success()
        {
            AccountStatus status = new AccountStatus(AccountKey.Parse("/account/", "/asset/"), 1, ByteString.Parse("ab"));

            Assert.Equal("/account/", status.AccountKey.Account.FullPath);
            Assert.Equal("/asset/", status.AccountKey.Asset.FullPath);
            Assert.Equal(1, status.Balance);
            Assert.Equal(ByteString.Parse("ab"), status.Version);
        }

        [Fact]
        public void AccountStatus_ArgumentNullException()
        {
            ArgumentNullException exception;

            exception = Assert.Throws<ArgumentNullException>(() => new AccountStatus(null, 1, ByteString.Parse("ab")));
            Assert.Equal("accountKey", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => new AccountStatus(AccountKey.Parse("/account/", "/asset/"), 1, null));
            Assert.Equal("version", exception.ParamName);
        }

        [Fact]
        public void FromRecord_Set()
        {
            Record record = new Record(
                AccountKey.Parse("/the/account/", "/the/asset/").Key.ToBinary(),
                SerializeInt(100),
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(RecordKey.Parse(record.Key), record);

            Assert.Equal("/the/account/", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset/", status.AccountKey.Asset.FullPath);
            Assert.Equal(100, status.Balance);
            Assert.Equal(binaryData[1], status.Version);
        }

        [Fact]
        public void FromRecord_Unset()
        {
            Record record = new Record(
                AccountKey.Parse("/the/account/", "/the/asset/").Key.ToBinary(),
                ByteString.Empty,
                binaryData[1]);

            AccountStatus status = AccountStatus.FromRecord(RecordKey.Parse(record.Key), record);

            Assert.Equal("/the/account/", status.AccountKey.Account.FullPath);
            Assert.Equal("/the/asset/", status.AccountKey.Asset.FullPath);
            Assert.Equal(0, status.Balance);
            Assert.Equal(binaryData[1], status.Version);
        }

        [Fact]
        public void FromRecord_InvalidRecordType()
        {
            RecordKey key = new RecordKey(RecordType.Data, LedgerPath.Parse("/path/"), "name");
            Record record = new Record(key.ToBinary(), ByteString.Empty, binaryData[1]);

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => AccountStatus.FromRecord(key, record));
            Assert.Equal("key", exception.ParamName);
        }

        private static ByteString SerializeInt(long value)
        {
            return new ByteString(BitConverter.GetBytes(value).Reverse());
        }
    }
}
