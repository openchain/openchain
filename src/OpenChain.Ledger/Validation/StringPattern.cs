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

namespace OpenChain.Ledger.Validation
{
    public class StringPattern
    {
        public static StringPattern MatchAll { get; } = new StringPattern("", PatternMatchingStrategy.Prefix);

        public StringPattern(string pattern, PatternMatchingStrategy matchingStrategy)
        {
            this.Pattern = pattern;
            this.MatchingStrategy = matchingStrategy;
        }
        
        public string Pattern { get; }

        public PatternMatchingStrategy MatchingStrategy { get; }

        public bool IsMatch(string value)
        {
            switch (MatchingStrategy)
            {
                case PatternMatchingStrategy.Exact:
                    return value == Pattern;
                case PatternMatchingStrategy.Prefix:
                    return value.StartsWith(Pattern, StringComparison.Ordinal);
                default:
                    throw new ArgumentOutOfRangeException(nameof(MatchingStrategy));
            }
        }
    }
}
