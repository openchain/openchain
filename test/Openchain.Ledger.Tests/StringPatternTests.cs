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
using Openchain.Ledger.Validation;
using Xunit;

namespace Openchain.Ledger.Tests
{
    public class StringPatternTests
    {
        [Fact]
        public void IsMatch_Success()
        {
            Assert.True(new StringPattern("name", PatternMatchingStrategy.Exact).IsMatch("name"));
            Assert.False(new StringPattern("name", PatternMatchingStrategy.Exact).IsMatch("name_suffix"));
            Assert.False(new StringPattern("name", PatternMatchingStrategy.Exact).IsMatch("nam"));
            Assert.False(new StringPattern("name", PatternMatchingStrategy.Exact).IsMatch("nams"));

            Assert.True(new StringPattern("name", PatternMatchingStrategy.Prefix).IsMatch("name"));
            Assert.True(new StringPattern("name", PatternMatchingStrategy.Prefix).IsMatch("name_suffix"));
            Assert.False(new StringPattern("name", PatternMatchingStrategy.Prefix).IsMatch("nam"));
            Assert.False(new StringPattern("name", PatternMatchingStrategy.Prefix).IsMatch("nams"));
        }

        [Fact]
        public void IsMatch_InvalidMatchingStrategy()
        {
            StringPattern pattern = new StringPattern("name", (PatternMatchingStrategy)1000);

            Assert.Throws<ArgumentOutOfRangeException>(() => pattern.IsMatch("name"));
        }
    }
}
