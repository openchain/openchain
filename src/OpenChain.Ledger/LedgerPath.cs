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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenChain.Ledger
{
    public class LedgerPath
    {
        // Only allow alphanumeric characters and characters in the following set: "$-_.+!*'(),".
        private static readonly Regex invalidCharacter = new Regex(@"[^A-Za-z0-9\$_\.\+!*'\(\),-]", RegexOptions.Compiled);

        private LedgerPath(string path, IEnumerable<string> segments)
        {
            this.FullPath = path;
            this.Segments = new ReadOnlyCollection<string>(segments.ToList());
        }

        public string FullPath { get; }

        public IReadOnlyList<string> Segments { get; }

        public static bool TryParse(string path, out LedgerPath result)
        {
            result = null;
            string[] segments = path.Split('/');
            if (segments.Length < 2 || segments[0] != string.Empty || segments[segments.Length - 1] != string.Empty)
                return false;

            if (segments.Any(segment => !IsValidPathSegment(segment)))
                return false;

            for (int i = 1; i < segments.Length - 1; i++)
                if (segments[i] == string.Empty)
                    return false;

            result = new LedgerPath(path, segments.Skip(1).Take(segments.Length - 2));

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

        public static LedgerPath FromSegments(params string[] segments)
        {
            if (segments == null || segments.Any(segment => !IsValidPathSegment(segment) || segment == string.Empty))
                throw new ArgumentOutOfRangeException(nameof(segments));

            return new LedgerPath("/" + string.Concat(segments.Select(segment => segment + "/")), segments);
        }

        public static bool IsValidPathSegment(string path)
        {
            return !invalidCharacter.IsMatch(path);
        }

        public static bool IsValidPath(string path)
        {
            return path.StartsWith("/", StringComparison.Ordinal) && path.Split('/').All(IsValidPathSegment);
        }

        public bool IsStrictParentOf(LedgerPath child)
        {
            if (child.Segments.Count <= this.Segments.Count)
                return false;

            for (int i = 0; i < this.Segments.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(this.Segments[i], child.Segments[i]))
                    return false;
            }

            return true;
        }

        public bool IsParentOf(LedgerPath child)
        {
            if (child.Segments.Count < this.Segments.Count)
                return false;

            for (int i = 0; i < this.Segments.Count; i++)
            {
                if (!StringComparer.Ordinal.Equals(this.Segments[i], child.Segments[i]))
                    return false;
            }

            return true;
        }
    }
}
