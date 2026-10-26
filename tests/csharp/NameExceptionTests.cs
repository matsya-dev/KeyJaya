// Noninteractive regressions: protected names, capitalization, and approved spelling exceptions.
// Run: .\test.ps1 -Suite NameExceptionTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Linq;

namespace KeyJaya
{
    public static class NameExceptionTests
    {
        static int passed;
        static void Equal(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new Exception(label + " expected=" + expected + " actual=" + actual);
            passed++;
        }

        public static int Main(string[] args)
        {
            using (Core core = new Core(args[0]))
            {
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    foreach (string name in new[]
                    {
                        "Bhaktivedanta",
                        "bhaktivedanta",
                        "BHAKTIVEDANTA"
                    }

                    )
                    {
                        Equal(name, core.Word(name, mode), "word " + mode);
                        Equal(name, core.ConvertCopied(name, mode), "copied " + mode);
                        Equal(name, core.ConvertHighlighted(name, mode), "selected " + mode);
                    }

                    Equal("Bhaktivedanta", Core.Target("Bhaktivedānta", mode), "IAST name exception " + mode);
                    Equal("Bhaktivedanta", Core.Target("Bhaktivedänta", mode), "legacy name exception " + mode);
                    Equal("Bhaktivedanta’s", core.ConvertCopied("Bhaktivedanta’s", mode), "possessive " + mode);
                    Equal("Earlier untouched Bhaktivedanta", core.ConvertCopied("Earlier untouched Bhaktivedanta", mode), "prose " + mode);
                    Equal("bhaktivedanta", core.ConvertItrans("bhaktivedAnta", mode), "ITRANS source " + mode);
                }

                Equal("bhaktivedanta", core.Word("Bhaktivedanta", "itrans"), "ITRANS output has no long-vowel code");
                Equal("bhaktivedanta", core.ConvertItrans("bhaktivedAnta", "itrans"), "ITRANS input normalized");
                foreach (char suffix in new[]
                {
                    '>',
                    '<',
                    '?'
                }

                )
                {
                    Equal("Bhaktivedanta", core.Trigger("Bhaktivedanta" + suffix), "suffix " + suffix);
                    foreach (string choice in core.Cycle("Bhaktivedanta", suffix))
                        Equal("Bhaktivedanta", choice, "cycle " + suffix);
                }

                Equal("śāstra", string.Join("|", core.ItalicWords("Bhaktivedanta and śāstra", "unicode").Select(span => "Bhaktivedanta and śāstra".Substring(span.Start, span.Length))), "name roman, adjacent term italic");
                Equal("Bhaktivedanta’s", Core.RomanNameExceptions("Bhaktivedānta’s"), "configured possessive");
                Equal("Bhaktivedantas", Core.RomanNameExceptions("Bhaktivedāntas"), "plural exception");
                Equal("https://example.org/Bhaktivedānta", Core.RomanNameExceptions("https://example.org/Bhaktivedānta"), "protected URL");
                Equal("भक्तिवेदान्त", Core.Target("bhaktivedānta", "devanagari"), "explicit script keeps Sanskrit vowels");
            }

            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
