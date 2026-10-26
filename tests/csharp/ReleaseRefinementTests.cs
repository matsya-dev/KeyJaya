// Noninteractive regressions: completion boundaries, title context, and accumulated regression cases.
// Run: .\test.ps1 -Suite ReleaseRefinementTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Linq;

namespace KeyJaya
{
    public static class ReleaseRefinementTests
    {
        static int pass;
        static void Check(bool ok, string name)
        {
            if (!ok)
                throw new Exception(name);
            pass++;
        }

        public static int Main(string[] args)
        {
            string file = Path.Combine(Path.GetDirectoryName(typeof(ReleaseRefinementTests).Assembly.Location), "plain-preference-" + Guid.NewGuid().ToString("N") + ".tsv");
            using (var core = new Core(args[0], file))
            {
                foreach (string word in new[]
                {
                    "doesnt",
                    "didnt",
                    "cant",
                    "shouldnt",
                    "lets",
                    "Lets"
                }

                )
                    foreach (char end in new[]
                    {
                        ' ',
                        '\t',
                        '\r',
                        '\n',
                        ',',
                        '.',
                        ';',
                        ':',
                        '!',
                        '?'
                    }

                    )
                    {
                        string input = word + end;
                        Check(core.MayCorrectCompletedWord(input), "preflight " + input);
                        string result = core.CorrectCompletedWord(input);
                        Check(result != null && result.EndsWith(end.ToString()) && result.Contains("’"), "completion " + input);
                    }

                foreach (string word in new[]
                {
                    "well",
                    "were",
                    "ill",
                    "hell",
                    "shell",
                    "its",
                    "wed",
                    "shed"
                }

                )
                    foreach (char end in new[]
                    {
                        ' ',
                        '\n',
                        ','
                    }

                    )
                        Check(core.CorrectCompletedWord(word + end) == null, "ambiguous " + word);
                Check(core.CtrlShort("SP-puja", "unicode") == "Śrīla Prabhupāda-pūjā", "short compound");
                Check(core.CtrlShort("sp-puja", "unicode") == null, "short exact case");
                foreach (string occurrence in new[]
                {
                    "one",
                    "two",
                    "three"
                }

                )
                {
                    Check(core.RememberCaretSpelling("rājā", "raja", "unicode", true, occurrence), "plain vote");
                    core.FinishSpellingChoice();
                }

                Check(core.Word("raja", "unicode") == "raja", "plain learned");
                Check(core.Word("räja", "balaram") == "raja", "plain shared");
            }

            var letters = new LetterCycle();
            letters.Observe('a', new IntPtr(5));
            for (int n = 0; n < 1000; n++)
            {
                Check(letters.Queue(n % 2 == 0 ? "unicode" : "balaram", new IntPtr(5)), "rapid queued");
                string mode, next;
                Check(letters.Take(out mode, out next), "rapid take");
                letters.Commit(next, mode);
                Check(letters.Original == "a", "letter origin");
            }

            letters.Clear();
            Check(letters.Original == null, "letter navigation reset");
            Check(MarkdownFormatting.Italicize("rāja and Kṛṣṇa", new[] { new ItalicSpan(0, 4) }) == "*rāja* and Kṛṣṇa", "markdown prose");
            Check(MarkdownFormatting.Italicize("rāja", new[] { new ItalicSpan(0, 4) }, "*", "*") == "rāja", "existing italics");
            Check(MarkdownFormatting.Italicize("rāja", new[] { new ItalicSpan(0, 4) }, "```\n") == "rāja", "fenced context");
            Check(MarkdownFormatting.Italicize("# rāja", new[] { new ItalicSpan(2, 4) }) == "# rāja", "heading roman");
            foreach (string text in new[]
            {
                "`raja`",
                "[raja](https://example.com)",
                "[[raja]]",
                "```\nraja\n```"
            }

            )
                Check(MarkdownFormatting.ConvertProtected(text, x => x.Replace("raja", "rāja")) == text, "protected markdown");
            Console.WriteLine("PASS: " + pass + " FAIL: 0");
            return 0;
        }
    }
}
