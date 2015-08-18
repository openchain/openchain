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
