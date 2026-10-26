// Noninteractive regressions: single-number cardinal/ordinal conversion and passage-number protection.
// Run: .\test.ps1 -Suite NumberCtrlTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Linq;

namespace KeyJaya
{
    public static class NumberCtrlTests
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
                string[, ] examples =
                {
                    {
                        "0",
                        "zero"
                    },
                    {
                        "1",
                        "one"
                    },
                    {
                        "24",
                        "twenty-four"
                    },
                    {
                        "24th",
                        "twenty-fourth"
                    },
                    {
                        "21st",
                        "twenty-first"
                    },
                    {
                        "22nd",
                        "twenty-second"
                    },
                    {
                        "23rd",
                        "twenty-third"
                    },
                    {
                        "100",
                        "one hundred"
                    },
                    {
                        "1,234",
                        "one thousand two hundred thirty-four"
                    },
                    {
                        "123rd",
                        "one hundred twenty-third"
                    }
                };
                for (int n = 0; n < examples.GetLength(0); n++)
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram",
                        "plain"
                    }

                    )
                    {
                        string input = examples[n, 0], expected = examples[n, 1];
                        Check(Numbers.Single(input) == expected, "independent number reference " + input);
                        Check(core.CaretChoices(input, mode).SequenceEqual(new[] { expected }), "single number Ctrl " + input + " " + mode);
                        Check(core.ConvertHighlighted(input, mode) == expected, "selected single number " + input);
                        Check(core.ConvertCopied(input, mode) == input, "copy shortcuts do not spell numbers");
                        Check(core.ItalicWords(expected, mode).Length == 0, "spelled number never gains Indic italics");
                        Check(core.Trigger(input + "#") == null && !core.IsDirectTrigger(input + "#"), "hash number trigger removed");
                        Check(core.ConvertHighlighted(input, mode, false) == input, "number in a passage fragment remains literal");
                        foreach (string prefix in new[]
                        {
                            "",
                            "Earlier untouched ",
                            "Earlier paragraph.\r\n("
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
                                string text = prefix + input + tail + " next";
                                for (int offset = 0; offset <= input.Length + tail.Length; offset++)
                                {
                                    int caret = prefix.Length + offset;
                                    var span = CaretWords.Locate(text, caret, caret);
                                    Check(span != null && span.Word == input && span.Start == prefix.Length && span.End == prefix.Length + input.Length, "exact numeric caret range " + input + " offset=" + offset);
                                    Check(text.Substring(0, span.Start) + core.CaretChoices(span.Word, mode)[0] + text.Substring(span.End) == prefix + expected + tail + " next", "neighbors and punctuation preserved");
                                    Check(span.Start + span.Offset == caret, "original numeric caret offset retained");
                                }

                                Check(CaretWords.AtEnd(prefix + input) == input, "mouse-paste number can be discovered");
                            }
                    }

                foreach (string invalid in new[]
                {
                    "-24",
                    "+24",
                    "1.5",
                    "12,34",
                    "1,00",
                    "18446744073709551616",
                    "24kg",
                    "x24",
                    "24/25",
                    "2026-10-22",
                    "12:30",
                    "https://example.com/24",
                    "C:/24",
                    "$24",
                    "24#",
                    "24::",
                    "24\n",
                    " 24",
                    "24 ",
                    "24 25",
                    "1st place"
                }

                )
                {
                    Check(Numbers.Single(invalid) == null, "only a complete supported number is accepted: " + invalid);
                    Check(core.CaretChoices(invalid, "unicode").Length == 0, "unsupported numeric Ctrl input unchanged: " + invalid);
                }

                foreach (string invalid in new[]
                {
                    "-24",
                    "+24",
                    "1.5",
                    "12,34",
                    "18446744073709551616",
                    "24kg",
                    "x24",
                    "24/25",
                    "2026-10-22",
                    "12:30",
                    "https://example.com/24",
                    "C:/24",
                    "$24",
                    "24#"
                }

                )
                    for (int caret = 0; caret <= invalid.Length; caret++)
                        Check(CaretWords.Locate(invalid, caret, caret) == null, "unsupported numeric token never partially edited: " + invalid);
                foreach (string passage in new[]
                {
                    "24 rāja and Kṛṣṇa 25th",
                    "Earlier untouched 24 and 1,234",
                    "24 25 26 27",
                    "BG 1.1 has 24 words in 2026.",
                    "1.5 and -24 and 2026-10-22",
                    "24\r\n25"
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
                        string output = core.ConvertHighlighted(passage, mode);
                        var expected = System.Text.RegularExpressions.Regex.Matches(passage, @"[0-9]+(?:[.,:-][0-9]+)*(?:st|nd|rd|th)?").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value);
                        var actual = System.Text.RegularExpressions.Regex.Matches(output, @"[0-9]+(?:[.,:-][0-9]+)*(?:st|nd|rd|th)?").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value);
                        Check(expected.SequenceEqual(actual), "passage numerals remain byte-for-byte unchanged");
                    }

                string markdown = "24 `code` 25 [link](https://example.com) 26 and rāja";
                Check(MarkdownFormatting.ConvertProtected(markdown, v => core.ConvertHighlighted(v, "unicode", false)) == markdown, "Markdown fragments cannot turn passage numerals into words");
                Check(core.ConvertHighlighted("24 Krsna", "unicode") == "24 Kṛṣṇa", "Indic conversion still works beside unchanged numbers");
                Check(core.ConvertCopied("24", "devanagari") == "२४" && core.ConvertCopied("24", "bengali") == "২৪", "explicit script digit conversion retained");
                Check(CaretWords.Locate("4REGS", 5, 5).Word == "4REGS", "numeric short abbreviation retained");
                Check(!core.HasShortAbbreviation("24"), "ordinary number has no abbreviation override");
                core.Short["24>"] = "Rāja";
                core.Short["24<"] = "Räja";
                Check(core.HasShortAbbreviation("24"), "numeric configured abbreviation detected before font-independent number conversion");
                Check(core.ConvertHighlighted("24", "unicode") == "Rāja" && core.ConvertHighlighted("24", "balaram") == "Räja", "configured numeric trigger takes priority in the requested encoding");
                core.Short.Remove("24>");
                core.Short.Remove("24<");
                var state = new InputState();
                foreach (char c in "24#")
                    Check(state.Type(c, core) == null || core.Trigger(state.CurrentLiteral) == null, "typing hash never expands number");
                state.RecordReplacement("24", "twenty-four", new IntPtr(4));
                Check(state.UndoOriginal == "24" && state.UndoOutput == "twenty-four", "manual number undo retains original target");
                Check(core.CaretChoices("an", "unicode").Length == 0 && core.CaretChoices("age", "unicode").Length == 0, "English guards retained");
            }

            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
