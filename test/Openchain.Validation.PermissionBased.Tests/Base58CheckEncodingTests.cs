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

using System.Linq;
using Xunit;

namespace Openchain.Validation.PermissionBased.Tests
{
    public class Base58CheckEncodingTests
    {
        [Fact]
        public void Encode_Success()
        {
            string data = Base58CheckEncoding.Encode(ByteString.Parse("6f" + "d5188210339b2012cd8f3c5ce3773d49dd7baa4b").Value.ToArray());
            Assert.Equal("mzwhamFUz1oFz2noTGDK9dxq3PAEhNkpuL", data);

            data = Base58CheckEncoding.Encode(ByteString.Parse("6f" + "4641508da141383ce2d1e035c58fad31480bcaac").Value.ToArray());
            Assert.Equal("mmvRqdAcJrSv6M8GozQE4tR3DhfF56c5M1", data);

            data = Base58CheckEncoding.Encode(ByteString.Parse("6f" + "eeab6b6757f135e9ec2f47157c412f80493f1eca").Value.ToArray());
            Assert.Equal("n3GvU9Zo74UZLYgHQcYiLgTFcWfErXFdwe", data);

            data = Base58CheckEncoding.Encode(ByteString.Parse("00" + "691290451961ad74e177bf44f32d9e2fe7454ee6").Value.ToArray());
            Assert.Equal("1AaaBxiLVzo1xZSFpAw3Zm9YBYAYQgQuuU", data);

            data = Base58CheckEncoding.Encode(ByteString.Parse("05" + "36e0ea8e93eaa0285d641305f4c81e563aa570a2").Value.ToArray());
            Assert.Equal("36hBrMeUfevFPZdY2iYSHVaP9jdLd9Np4R", data);
        }

        [Fact]
        public void Decode_Success()
        {
            ByteString data = new ByteString(Base58CheckEncoding.Decode("mzwhamFUz1oFz2noTGDK9dxq3PAEhNkpuL"));
            Assert.Equal(ByteString.Parse("6f" + "d5188210339b2012cd8f3c5ce3773d49dd7baa4b"), data);

            data = new ByteString(Base58CheckEncoding.Decode("mmvRqdAcJrSv6M8GozQE4tR3DhfF56c5M1"));
            Assert.Equal(ByteString.Parse("6f" + "4641508da141383ce2d1e035c58fad31480bcaac"), data);

            data = new ByteString(Base58CheckEncoding.Decode("n3GvU9Zo74UZLYgHQcYiLgTFcWfErXFdwe"));
            Assert.Equal(ByteString.Parse("6f" + "eeab6b6757f135e9ec2f47157c412f80493f1eca"), data);

            data = new ByteString(Base58CheckEncoding.Decode("1AaaBxiLVzo1xZSFpAw3Zm9YBYAYQgQuuU"));
            Assert.Equal(ByteString.Parse("00" + "691290451961ad74e177bf44f32d9e2fe7454ee6"), data);
        }
    }
}
