// KeyJaya: Source-only file policy for packaging and reversible upgrade cleanup.
// Configuration, executables and runtime dictionaries cannot be retired by this list.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KeyJaya
{
    internal static class DistributionFiles
    {
        public const string Manifest = "distribution-exclusions.txt";
        internal static bool Valid(string relative)
        {
            // Windows can normalize a trailing period or resolve an 8.3 alias
            // to a protected dictionary despite a different-looking extension.
            if (string.IsNullOrEmpty(relative) || relative.IndexOfAny(new[] { '\\', ':', '\0', '*', '?', '~' }) >= 0)
                return false;
            foreach (char c in relative)
                if (char.IsControl(c))
                    return false;
            if (relative.Equals("THIRD_PARTY_NOTICES.md", StringComparison.OrdinalIgnoreCase))
                return true;
            string[] parts = relative.Split('/');
            if (parts.Length != 2 || parts[1] == "." || parts[1] == ".." || parts[1].Length == 0 || parts[1].EndsWith(".", StringComparison.Ordinal) || !(parts[0].Equals("data", StringComparison.OrdinalIgnoreCase) || parts[0].Equals("docs", StringComparison.OrdinalIgnoreCase)))
                return false;
            // Never interpret a source-only policy as permission to remove live
            // dictionaries or editable/user-owned files, even after a bad edit.
            string extension = Path.GetExtension(parts[1]).ToLowerInvariant();
            return Array.IndexOf(new[] { ".dat", ".idx", ".hash", ".tsv", ".ini", ".exe", ".dll", ".ico" }, extension) < 0;
        }

        public static string[] Read(string path)
        {
            var files = new List<string>();
            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                string relative = raw.Trim();
                if (relative.Length == 0 || relative.StartsWith("#", StringComparison.Ordinal))
                    continue;
                if (!Valid(relative) || !unique.Add(relative))
                    throw new InvalidDataException("Invalid or duplicate distribution exclusion: " + relative);
                files.Add(relative);
            }

            return files.ToArray();
        }
    }
}
