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

namespace Openchain.Validation.PermissionBased
{
    /// <summary>
    /// Represents a pattern for string matching.
    /// </summary>
    public class StringPattern
    {
        /// <summary>
        /// Gets a pattern matching all strings.
        /// </summary>
        public static StringPattern MatchAll { get; } = new StringPattern("", PatternMatchingStrategy.Prefix);

        public StringPattern(string pattern, PatternMatchingStrategy matchingStrategy)
        {
            this.Pattern = pattern;
            this.MatchingStrategy = matchingStrategy;
        }

        /// <summary>
        /// Gets the pattern string.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets the matching strategy.
        /// </summary>
        public PatternMatchingStrategy MatchingStrategy { get; }

        /// <summary>
        /// Checks if a string matches the pattern.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>A boolean indicating whether the value matches.</returns>
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
