using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenChain.Ledger
{
    public class LedgerPath
    {
        // Only allow alphanumeric characters and characters in the following set: "$-_.+!*'(),".
        private static readonly Regex invalidCharacter = new Regex(@"[^\w\$_\.\+!*'\(\),-]", RegexOptions.Compiled);

        private LedgerPath(string path, IEnumerable<string> segments, bool isDirectory)
        {
            this.FullPath = path;
            this.Segments = new ReadOnlyCollection<string>(segments.ToList());
            this.IsDirectory = isDirectory;
        }

        public static bool TryParse(string path, out LedgerPath result)
        {
            result = null;
            string[] segments = path.Split('/');
            if (segments.Length < 2 || segments[0] != string.Empty)
                return false;

            if (segments.Any(segment => !IsValidPathSegment(segment)))
                return false;

            for (int i = 1; i < segments.Length - 1; i++)
                if (segments[i] == string.Empty)
                    return false;

            if (segments[segments.Length - 1] == string.Empty)
                result = new LedgerPath(path, segments.Skip(1).Take(segments.Length - 2), true);
            else
                result = new LedgerPath(path, segments.Skip(1), false);

            return true;
        }

        public static LedgerPath Parse(string path)
        {
            LedgerPath result;
            if (TryParse(path, out result))
                return result;
            else
                throw new ArgumentOutOfRangeException(nameof(path));
        }

        public static LedgerPath FromSegments(string[] segments, bool isDirectory)
        {
            if (segments == null || segments.Any(segment => !IsValidPathSegment(segment) || segment == string.Empty))
                throw new ArgumentOutOfRangeException(nameof(segments));

            return new LedgerPath(
                "/" + string.Join("/", segments) + (isDirectory ? "/" : ""),
                segments,
                isDirectory);
        }

        public static bool IsValidPathSegment(string path)
        {
            //return !path.Contains("\0") && !path.Contains("/");
            return !invalidCharacter.IsMatch(path);
        }

        public static bool IsValidPath(string path)
        {
            return path.StartsWith("/", StringComparison.Ordinal) && path.Split('/').All(IsValidPathSegment);
        }

        public string FullPath { get; }

        public IReadOnlyList<string> Segments { get; }
        
        public bool IsDirectory { get; }
    }
}
