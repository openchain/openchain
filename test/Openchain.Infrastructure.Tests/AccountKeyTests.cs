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
using Xunit;

namespace Openchain.Infrastructure.Tests
{
    public class AccountKeyTests
    {
        [Fact]
        public void AccountKey_Success()
        {
            AccountKey result = new AccountKey(LedgerPath.Parse("/path/"), LedgerPath.Parse("/asset/"));

            Assert.Equal("/path/", result.Account.FullPath);
            Assert.Equal("/asset/", result.Asset.FullPath);
        }

        [Fact]
        public void AccountKey_ArgumentNullException()
        {
            ArgumentNullException exception;

            exception = Assert.Throws<ArgumentNullException>(() => new AccountKey(null, LedgerPath.Parse("/asset/")));
            Assert.Equal("account", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => new AccountKey(LedgerPath.Parse("/path/"), null));
            Assert.Equal("asset", exception.ParamName);
        }

        [Fact]
        public void Equals_Success()
        {
            Assert.True(AccountKey.Parse("/abc/", "/def/").Equals(AccountKey.Parse("/abc/", "/def/")));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(AccountKey.Parse("/abc/", "/ghi/")));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(null));
            Assert.False(AccountKey.Parse("/abc/", "/def/").Equals(100));
        }

        [Fact]
        public void GetHashCode_Success()
        {
            Assert.Equal(AccountKey.Parse("/abc/", "/def/").GetHashCode(), AccountKey.Parse("/abc/", "/def/").GetHashCode());
        }
    }
}
