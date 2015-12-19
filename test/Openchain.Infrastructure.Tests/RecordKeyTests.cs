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
using System.Text;
using Xunit;

namespace Openchain.Infrastructure.Tests
{
    public class RecordKeyTests
    {
        private readonly ByteString[] binaryData =
            Enumerable.Range(0, 10).Select(index => new ByteString(Enumerable.Range(0, 32).Select(i => (byte)index))).ToArray();

        [Fact]
        public void Parse_Account()
        {
            ByteString data = new ByteString(Encoding.UTF8.GetBytes("/account/name/:ACC:/asset/name/"));
            RecordKey key = RecordKey.Parse(data);

            Assert.Equal(RecordType.Account, key.RecordType);
            Assert.Equal("/account/name/", key.Path.FullPath);
            Assert.Equal("/asset/name/", key.Name);
        }

        [Fact]
        public void Parse_Data()
        {
            ByteString data = new ByteString(Encoding.UTF8.GetBytes("/aka/name/:DATA:record:name"));
            RecordKey key = RecordKey.Parse(data);

            Assert.Equal(RecordType.Data, key.RecordType);
            Assert.Equal("/aka/name/", key.Path.FullPath);
            Assert.Equal("record:name", key.Name);
        }

        [Fact]
        public void Parse_Error()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => RecordKey.Parse(null));
            Assert.Equal("keyData", exception.ParamName);

            // Invalid structure
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("/account/name/"))));

            // Unknown record type
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("/account/name/:DOESNOTEXIST:"))));

            // Incorrect number of additional components
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("/asset/name/:ACC:/other/:other"))));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("/asset/name/:ACC"))));

            // Invalid path
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("account/name/:ACC:/"))));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecordKey.Parse(new ByteString(Encoding.UTF8.GetBytes("/account/name/:ACC:account"))));
        }

        [Fact]
        public void GetRecordTypeName_Success()
        {
            Assert.Equal("ACC", RecordKey.GetRecordTypeName(RecordType.Account));
            Assert.Equal("DATA", RecordKey.GetRecordTypeName(RecordType.Data));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecordKey.GetRecordTypeName((RecordType)100000));
        }

        private static ByteString SerializeInt(long value)
        {
            return new ByteString(BitConverter.GetBytes(value).Reverse());
        }
    }
}
