// Noninteractive regressions: learning a preference only across distinct word occurrences.
// Run: .\test.ps1 -Suite OccurrencePreferenceTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;

namespace KeyJaya
{
    public static class OccurrencePreferenceTests
    {
        public static int Main(string[] args)
        {
            int passed = 0;
            string path = Path.Combine(Path.GetDirectoryName(typeof(OccurrencePreferenceTests).Assembly.Location), "KeyJaya-occurrence-" + Guid.NewGuid().ToString("N") + ".tsv");
            using (Core core = new Core(args[0], path))
            {
                for (int n = 0; n < 5; n++)
                {
                    if (!core.RememberSpelling("rāja", "rājā", "unicode", "auto", "same occurrence"))
                        throw new Exception("Choice not staged");
                    core.FinishSpellingChoice();
                    if (core.Word("rāja", "unicode") != "rāja")
                        throw new Exception("One occurrence learned repeatedly");
                    passed++;
                }

                core.RememberSpelling("rāja", "rājā", "unicode", "auto", "second occurrence");
                core.FinishSpellingChoice();
                if (core.Word("rāja", "unicode") != "rāja")
                    throw new Exception("Two occurrences learned early");
                passed++;
                core.RememberSpelling("rāja", "rājā", "unicode", "auto", "third occurrence");
                core.FinishSpellingChoice();
                if (core.Word("rāja", "unicode") != "rājā")
                    throw new Exception("Three occurrences not learned");
                passed++;
            }

            var cycle = new CaretWordCycle();
            var owner = new IntPtr(8);
            int[] focus =
            {
                1,
                2
            };
            cycle.Start("rāja", new[] { "rāja", "rājā", "raja" }, "unicode", owner, focus, "raja");
            string next;
            for (int n = 0; n < 8; n++)
            {
                if (!cycle.Next(cycle.Expected(owner, focus), "unicode", owner, focus, out next))
                    throw new Exception("Cycle ended");
                cycle.Commit(next);
                if (cycle.Original != "raja")
                    throw new Exception("Cycle original changed");
                passed++;
            }

            cycle.Clear();
            if (cycle.Original != null)
                throw new Exception("Original survived reset");
            passed++;
            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
