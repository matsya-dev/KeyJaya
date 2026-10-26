// Noninteractive regressions: slash-separated prose targets versus protected URLs and paths.
// Run: .\test.ps1 -Suite SlashWordTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Linq;

namespace KeyJaya
{
    public static class SlashWordTests
    {
        static int passed;
        static void Check(bool ok, string label)
        {
            if (!ok)
                throw new Exception(label);
            passed++;
        }

        public static int Main(string[] args)
        {
            using (var core = new Core(args[0]))
            {
                foreach (string input in new[]
                {
                    "pandit",
                    "Pandit",
                    "pundit",
                    "Pundit"
                }

                )
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram",
                        "plain"
                    }

                    )
                    {
                        string unicode = char.IsUpper(input[0]) ? "Paṇḍita" : "paṇḍita", expected = Core.Target(unicode, mode);
                        Check(core.Word(input, mode) == expected, "curated alias " + input + " " + mode);
                        Check(core.ConvertCopied(input, mode) == expected, "copied alias " + input + " " + mode);
                        Check(core.ConvertHighlighted(input, mode) == expected, "selected alias " + input + " " + mode);
                        Check(core.CaretChoices(input, mode).First() == expected, "first Ctrl choice " + input + " " + mode);
                        Check(core.Trigger(input + (mode == "balaram" ? "<" : mode == "plain" ? "?" : ">")) == expected, "explicit alias " + input + " " + mode);
                    }

                foreach (string prefix in new[]
                {
                    "",
                    "Earlier untouched ",
                    "Earlier paragraph.\r\n("
                }

                )
                    foreach (string pair in new[]
                    {
                        "God/Krsna",
                        "pandit/pundit",
                        "Krsna/Rama",
                        "God/Vyasa-puja"
                    }

                    )
                        foreach (string tail in new[]
                        {
                            "",
                            ",",
                            ",)",
                            "?!"
                        }

                        )
                        {
                            int slash = pair.IndexOf('/');
                            string text = prefix + pair + tail + " next";
                            for (int offset = 0; offset <= pair.Length + tail.Length; offset++)
                            {
                                int caret = prefix.Length + offset;
                                var span = CaretWords.Locate(text, caret, caret);
                                Check(span != null, "slash target available " + pair + " offset=" + offset);
                                int sideStart = offset <= slash ? prefix.Length : prefix.Length + slash + 1, sideEnd = offset <= slash ? prefix.Length + slash : prefix.Length + pair.Length;
                                Check(span.Start >= sideStart && span.End <= sideEnd, "slash never joins two targets");
                                string converted = core.Word(span.Word, "unicode"), changed = text.Substring(0, span.Start) + converted + text.Substring(span.End);
                                Check(changed.Substring(0, sideStart) == text.Substring(0, sideStart) && changed.EndsWith(text.Substring(sideEnd), StringComparison.Ordinal), "other slash words and punctuation unchanged");
                                Check(span.Start + span.Offset == caret, "caret restoration retains slash offset");
                            }

                            Check(CaretWords.AtEnd(prefix + pair) == pair.Substring(slash + 1), "paste detection finds last slash word");
                        }

                string example = "Earlier untouched God/Krsna";
                var target = CaretWords.Locate(example, example.Length, example.Length);
                Check(target.Word == "Krsna" && example.Substring(0, target.Start) + core.Word(target.Word, "unicode") == "Earlier untouched God/Kṛṣṇa", "requested God/Krsna result");
                foreach (string invalid in new[]
                {
                    "https://example.com/Krsna",
                    "www.example.com/Krsna",
                    "person@God/Krsna",
                    "C:/God/Krsna",
                    "C:\\God/Krsna",
                    "/God/Krsna",
                    "./God/Krsna",
                    "../God/Krsna",
                    "God//Krsna",
                    "God/Krsna.txt",
                    "God_name/Krsna",
                    "#God/Krsna",
                    "God/Krsna/"
                }

                )
                {
                    for (int caret = 0; caret <= invalid.Length; caret++)
                        Check(CaretWords.Locate(invalid, caret, caret) == null, "protected slash token " + invalid);
                    Check(CaretWords.AtEnd(invalid) == null, "protected clipboard token " + invalid);
                }

                Check(CaretWords.Locate("God/Krsna ", 10, 10) == null, "trailing whitespace has no word");
                Check(CaretWords.Locate(example, target.Start, target.End) == null, "caret detector never captures a selection");
                var cycle = new CaretWordCycle();
                var owner = new IntPtr(2);
                int[] focus =
                {
                    7
                };
                string original = "Krsna", word = core.CaretChoices(original, "unicode")[0];
                cycle.Start(word, core.CaretChoices(original, "unicode"), "unicode", owner, focus, original);
                for (int n = 0; n < 30; n++)
                {
                    string text = "God/" + word;
                    var span = CaretWords.Locate(text, text.Length, text.Length);
                    Check(span != null && span.Start == 4 && span.Word == word, "repeated cycle retains slash range");
                    string next;
                    Check(cycle.Next(word, "unicode", owner, focus, out next), "slash cycle continues");
                    cycle.Commit(next);
                    word = next;
                    Check(cycle.Original == original, "undo retains the original slash component");
                }

                Check(core.ConvertCopied("an age since God", "unicode") == "an age since God", "ordinary English protection unchanged");
            }

            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
