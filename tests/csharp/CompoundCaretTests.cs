// Noninteractive regressions: compound/slash caret positions, punctuation, and target boundaries.
// Run: .\test.ps1 -Suite CompoundCaretTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Linq;

namespace KeyJaya
{
    public static class CompoundCaretTests
    {
        static int passed;
        static void Check(bool ok, string label)
        {
            if (!ok)
                throw new Exception(label);
            passed++;
        }

        static void RichCaretRanges()
        {
            foreach (string word in new[]
            {
                "raja",
                "rāja",
                "rājā",
                "jaya",
                "jayā",
                "jāya",
                "śāstra",
                "kṛṣṇa",
                "Vyāsa-pūjā",
                "24",
                "BG2.13–14VT",
                new string ('a', 160)
            }

            )
            {
                for (int offset = 0; offset <= word.Length; offset++)
                {
                    string before = word.Substring(0, offset), after = word.Substring(offset);
                    Check(CaretWords.RangeAtCaret(word, offset, "", before, after), "Exact text on both sides proves a caret inside a formatted word");
                    Check(!CaretWords.RangeAtCaret(word, offset, "", "previous " + before, after), "An earlier repeated word cannot claim the caret");
                    Check(!CaretWords.RangeAtCaret(word, offset, "", before, after + "Plain-text editor"), "A neighboring editor or heading is excluded");
                    Check(!CaretWords.RangeAtCaret(word, offset, "", before + "x", after), "A provider character-offset mismatch is rejected");
                }

                foreach (string tail in new[]
                {
                    ".",
                    ",",
                    "),",
                    "?!",
                    "’"
                }

                )
                {
                    Check(CaretWords.RangeAtCaret(word, word.Length + tail.Length, tail, word + tail, ""), "Verified punctuation remains outside the replaced word");
                    Check(!CaretWords.RangeAtCaret(word, word.Length + tail.Length, tail, word + tail, "next"), "Following text cannot be mistaken for the punctuation tail");
                    Check(!CaretWords.RangeAtCaret(word, word.Length + tail.Length, tail, word, ""), "A missing punctuation seam does not authorize a range");
                }
            }

            foreach (string tail in new[]
            {
                " ",
                "next",
                "/",
                "\\",
                "@",
                "#",
                new string ('.', 33)
            }

            )
                Check(!CaretWords.RangeAtCaret("raja", 4 + tail.Length, tail, "raja" + tail, ""), "Unsafe or oversized tails are rejected");
            Check(!CaretWords.RangeAtCaret(null, 0, "", "", "") && !CaretWords.RangeAtCaret("", 0, "", "", "") && !CaretWords.RangeAtCaret(new string ('a', 161), 0, "", "", new string ('a', 161)), "Invalid or oversized word ranges are rejected");
            Check(!CaretWords.RangeAtCaret("raja", -1, "", "", "") && !CaretWords.RangeAtCaret("raja", 5, "", "raja", "") && !CaretWords.RangeAtCaret("raja", 4, null, "raja", ""), "Invalid caret coordinates are rejected");
            string context = "rājā\nPlain-text editor";
            var span = CaretWords.Locate(context, 4, 4);
            Check(span != null && span.Word == "rājā" && CaretWords.RangeAtCaret(span.Word, span.Offset, span.Tail, "rājā", ""), "A contiguous context retains the rich-editor paragraph separator");
            var merged = CaretWords.Locate("rājāPlain-text editor", 4, 4);
            Check(merged != null && !CaretWords.RangeAtCaret(merged.Word, merged.Offset, merged.Tail, "rājā", ""), "A lost separator cannot expand the editable word into a following heading");
            foreach (string word in new[]
            {
                "rāja",
                "jaya",
                "Vyāsa-pūjā"
            }

            )
                foreach (string prefix in new[]
                {
                    "",
                    word + " ",
                    word + " earlier " + word + " "
                }

                )
                {
                    string text = prefix + word + " after " + word;
                    int start = prefix.Length;
                    for (int offset = 0; offset <= word.Length; offset++)
                    {
                        int caret = start + offset;
                        var target = CaretWords.Locate(text, caret, caret);
                        if (target == null || target.Word != word)
                            continue;
                        foreach (System.Text.RegularExpressions.Match candidate in System.Text.RegularExpressions.Regex.Matches(text, System.Text.RegularExpressions.Regex.Escape(word)))
                        {
                            if (candidate.Index > caret)
                                continue;
                            int end = candidate.Index + candidate.Length;
                            string before = text.Substring(candidate.Index, caret - candidate.Index), after = caret < end ? text.Substring(caret, end - caret) : "";
                            Check(CaretWords.RangeAtCaret(word, offset, "", before, after) == (candidate.Index == start), "Repeated spellings authorize only the candidate at the actual caret");
                        }
                    }
                }
        }

        public static int Main(string[] args)
        {
            RichCaretRanges();
            using (Core core = new Core(args[0]))
            {
                foreach (string separator in new[]
                {
                    "-",
                    "\u2011",
                    "\u2013"
                }

                )
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
                            string compound = "Vyasa" + separator + "puja", text = prefix + compound + tail + " next";
                            for (int offset = 0; offset <= compound.Length + tail.Length; offset++)
                            {
                                int caret = prefix.Length + offset;
                                var span = CaretWords.Locate(text, caret, caret);
                                string expected;
                                int start, end;
                                if (offset <= 5)
                                {
                                    expected = "Vyasa";
                                    start = prefix.Length;
                                    end = start + 5;
                                }
                                else if (offset < 10 || offset > 10)
                                {
                                    expected = "puja";
                                    start = prefix.Length + 6;
                                    end = start + 4;
                                }
                                else
                                {
                                    expected = compound;
                                    start = prefix.Length;
                                    end = start + 10;
                                }

                                Check(span != null && span.Word == expected && span.Start == start && span.End == end, "exact component/whole target offset=" + offset + " separator=" + separator);
                                string converted = core.Word(span.Word, "unicode"), changed = text.Substring(0, span.Start) + converted + text.Substring(span.End);
                                string wanted = text.Substring(0, start) + converted + text.Substring(end);
                                Check(changed == wanted, "only intended range changed");
                                int restoredCaret = start + span.Offset;
                                Check(restoredCaret == caret, "original caret saved independently of target length");
                                if (offset > 10)
                                    Check(span.Tail == tail.Substring(0, offset - 10), "punctuation belongs to caret only, not replaced text");
                            }

                            var component = CaretWords.Locate(text, prefix.Length + 10, prefix.Length + 10, true);
                            Check(component != null && component.Word == "puja", "internal replacement positioning can locate final component at collapsed caret");
                        }

                foreach (string invalid in new[]
                {
                    "https://example.com/Vyasa-puja",
                    "person@Vyasa-puja",
                    "C:\\Vyasa-puja",
                    "#Vyasa-puja",
                    "Vyasa-puja ",
                    "Vyasa--puja"
                }

                )
                    Check(CaretWords.Locate(invalid, invalid.Length, invalid.Length) == null, "unsafe boundary rejected " + invalid);
                var state = new InputState();
                string trigger = null;
                foreach (char c in "Earlier untouched Vyasa\u2011puja>")
                {
                    string next = state.Type(c);
                    if (next != null)
                        trigger = next;
                }

                Check(trigger == "Vyasa\u2011puja>", "nonbreaking-hyphen physical trigger stays whole");
                var cycle = new CaretWordCycle();
                var owner = new IntPtr(2);
                int[] focus =
                {
                    5
                };
                cycle.Start("pūjā", new[] { "pūjā", "puja" }, "unicode", owner, focus, "puja");
                string nextChoice;
                for (int n = 0; n < 20; n++)
                {
                    Check(cycle.Next(cycle.Expected(owner, focus), "unicode", owner, focus, out nextChoice), "component cycle remains continuous");
                    cycle.Commit(nextChoice);
                    Check(cycle.Original == "puja", "component undo origin never changes");
                }

                Check(core.CaretChoices("puja", "unicode").Contains("pūjā"), "isolated puja component has diacritics");
                Check(core.Word("Vyasa", "unicode") == "Vyāsa", "isolated Vyasa component has diacritics");
                string[, ] reference =
                {
                    {
                        "vyasa-puja",
                        "Vyāsa-pūjā"
                    },
                    {
                        "vaidhi-bhakti",
                        "vaidhī-bhakti"
                    },
                    {
                        "sac-cid-ananda-vigraha",
                        "sac-cid-ānanda-vigraha"
                    },
                    {
                        "nama-sankirtana",
                        "nāma-saṅkīrtana"
                    },
                    {
                        "varnaśrama-dharma",
                        "varṇāśrama-dharma"
                    },
                    {
                        "navadvipa-dhama",
                        "Navadvīpa-dhāma"
                    },
                    {
                        "founder-acarya",
                        "founder-ācārya"
                    }
                };
                for (int n = 0; n < reference.GetLength(0); n++)
                    Check(core.ConvertCopied(reference[n, 0], "unicode") == reference[n, 1], "independent book spelling " + reference[n, 0]);
            }

            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
