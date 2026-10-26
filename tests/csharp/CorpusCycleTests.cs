// Noninteractive regressions: corpus spelling variants across output encodings.
// Run: .\test.ps1 -Suite CorpusCycleTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KeyJaya
{
    public static class CorpusCycleTests
    {
        static long passed, failed;
        static List<string> errors = new List<string>();
        static void Check(bool ok, string name)
        {
            if (ok)
                passed++;
            else
            {
                failed++;
                if (errors.Count < 30)
                    errors.Add(name);
            }
        }

        public static int Main(string[] args)
        {
            using (Core c = new Core(args[0]))
            {
                string folder = Path.Combine(args[0], "data");
                byte[] data = File.ReadAllBytes(Path.Combine(folder, "cycle.dat"));
                IntPtr owner = new IntPtr(55);
                int[] field =
                {
                    99,
                    45,
                    10
                };
                DateTime now = new DateTime(2026, 10, 11);
                int words = 0, combinations = 0;
                using (BinaryReader reader = new BinaryReader(File.OpenRead(Path.Combine(folder, "cycle.idx"))))
                {
                    int row = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        reader.ReadUInt64();
                        long offset = reader.ReadInt64();
                        int length = reader.ReadInt32();
                        if (row++ % 5 != 0)
                            continue;
                        string record = Encoding.UTF8.GetString(data, (int)offset, length), key = record.Substring(0, record.IndexOf('\0'));
                        if (!Regex.IsMatch(key, @"^[a-z]+$") || c.English(key) && !Core.IndicLoanwords.Contains(key))
                            continue;
                        string word = c.Word(key, "unicode");
                        if (word == key || !EncodingMap.IsIndic(word))
                            continue;
                        bool cycled = false;
                        foreach (string mode in new[]
                        {
                            "unicode",
                            "balaram",
                            "plain",
                            "itrans"
                        }

                        )
                        {
                            string[] options = c.ClipboardChoices(word, mode, "auto");
                            if (options.Length < 2)
                                continue;
                            cycled = true;
                            combinations++;
                            ClipboardCycle cycle = new ClipboardCycle();
                            cycle.Start(options[0], options, mode, "auto", 1, owner, field, now);
                            for (int n = 1; n <= options.Length * 2; n++)
                            {
                                string output;
                                Check(cycle.Next(mode, "auto", n == 1 ? options[0] : "", (uint)n, owner, field, now, out output), "repeated hotkey has next " + key + " " + mode);
                                Check(output == options[n % options.Length], "corpus cycle order/wrap " + key + " " + mode);
                                cycle.Commit(output, (uint)n + 1, now);
                                cycle.Released((uint)n + 1);
                                int begin;
                                string prefix = "an age since untouched. ", suffix = " text after";
                                string document = prefix + output + suffix;
                                int caret = prefix.Length + output.Length;
                                Check(EditGuard.Locate(document, caret, caret, output, out begin) && begin == prefix.Length, "owned replacement only " + key);
                                Check(document.Substring(0, begin) == prefix && document.Substring(caret) == suffix, "English on both sides unchanged " + key);
                            }
                        }

                        if (cycled)
                            words++;
                    }
                }

                foreach (string word in File.ReadLines(Path.Combine(folder, "english-guard.txt")))
                {
                    if (Core.IndicLoanwords.Contains(word))
                        continue;
                    foreach (string form in new[]
                    {
                        char.ToUpperInvariant(word[0]) + word.Substring(1),
                        word.ToUpperInvariant(),
                        word + "'s",
                        word + "’s"
                    }

                    )
                        foreach (string mode in new[]
                        {
                            "unicode",
                            "balaram",
                            "plain",
                            "itrans"
                        }

                        )
                            Check(c.ClipboardChoices(form, mode, "auto").Length == 0, "English form never cycles " + form + " " + mode);
                }

                Check(words > 1000, "more than a thousand corpus words cycled");
                Console.WriteLine("Corpus Sanskrit words cycled: " + words + " word/format combinations: " + combinations);
            }

            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            foreach (string error in errors)
                Console.WriteLine("FAIL " + error);
            return failed == 0 ? 0 : 1;
        }
    }
}
