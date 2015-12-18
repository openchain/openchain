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

using Xunit;

namespace Openchain.Validation.PermissionBased.Tests
{
    public class KeyEncoderTests
    {
        [Fact]
        public void IsP2pkh_Success()
        {
            KeyEncoder keyEncoder = new KeyEncoder(111);

            // Valid
            Assert.Equal(true, keyEncoder.IsP2pkh("mfiCwNxuFYMtb5ytCacgzDAineD2GNCnYo"));
            // Wrong checksum
            Assert.Equal(false, keyEncoder.IsP2pkh("mfiCwNxuFYMtb5ytCacgzDAineD2GNCnYp"));
            // Invalid size
            Assert.Equal(false, keyEncoder.IsP2pkh("2Qx7aDxjSh772"));
            // Empty
            Assert.Equal(false, keyEncoder.IsP2pkh(""));
            // Invalid version byte
            Assert.Equal(false, keyEncoder.IsP2pkh("1CCW1yPxC8meB7JzF8xEwaad4DxksFqhrQ"));
        }

        [Fact]
        public void GetPubKeyHash_Success()
        {
            KeyEncoder keyEncoder = new KeyEncoder(111);

            Assert.Equal("n12RA1iohYEerfXiBixSoERZG8TP8xQFL2", keyEncoder.GetPubKeyHash(ByteString.Parse("abcdef")));
        }
    }
}
