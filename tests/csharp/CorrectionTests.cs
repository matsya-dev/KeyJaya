// Noninteractive regressions: spelling/contraction dictionary behavior over completed-word inputs.
// Run: .\test.ps1 -Suite CorrectionTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace KeyJaya
{
    public static class CorrectionTests
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
                if (errors.Count < 150 && !errors.Contains(name))
                    errors.Add(name);
            }
        }

        static void Equal(string expected, string actual, string name)
        {
            Check(expected == actual, name + " expected=" + expected + " actual=" + actual);
        }

        public static int Main(string[] args)
        {
            using (Core c = new Core(args[0]))
            {
                foreach (string pair in new[]
                {
                    "doesnt|doesn’t",
                    "dont|don’t",
                    "cant|can’t",
                    "isnt|isn’t",
                    "wasnt|wasn’t",
                    "wouldnt|wouldn’t",
                    "doesn't|doesn’t",
                    "shouldnt|shouldn’t",
                    "shouldve|should’ve",
                    "shes|she’s",
                    "shant|shan’t",
                    "thats|that’s",
                    "couldnt|couldn’t",
                    "didnt|didn’t",
                    "arent|aren’t",
                    "werent|weren’t",
                    "hadnt|hadn’t",
                    "hasnt|hasn’t",
                    "havent|haven’t",
                    "mightnt|mightn’t",
                    "mustnt|mustn’t",
                    "neednt|needn’t",
                    "oughtnt|oughtn’t"
                }

                )
                {
                    string[] x = pair.Split('|');
                    foreach (string separator in new[]
                    {
                        " ",
                        "\t"
                    }

                    )
                        Equal(x[1] + separator, c.CorrectCompletedWord(x[0] + separator), "independent common contraction coverage " + x[0]);
                }

                foreach (string pair in new[]
                {
                    "shouldnt|shouldn’t",
                    "shes|she’s",
                    "shouldve|should’ve",
                    "shant|shan’t",
                    "thats|that’s"
                }

                )
                {
                    string[] x = pair.Split('|');
                    Equal(x[1] + " ", c.CorrectCompletedWord(x[1].Replace('’', '\'') + " "), "straight apostrophe normalized " + x[0]);
                    Equal(null, c.CorrectCompletedWord(x[1] + " "), "correct curly form untouched " + x[0]);
                    Equal(char.ToUpperInvariant(x[1][0]) + x[1].Substring(1) + " ", c.CorrectCompletedWord(char.ToUpperInvariant(x[0][0]) + x[0].Substring(1) + " "), "capitalized common contraction " + x[0]);
                    Equal(x[1].ToUpperInvariant() + " ", c.CorrectCompletedWord(x[0].ToUpperInvariant() + " "), "uppercase common contraction " + x[0]);
                }

                foreach (string pair in new[]
                {
                    "lets|let’s",
                    "Lets|Let’s",
                    "LETS|LET’S"
                }

                )
                {
                    string[] forms = pair.Split('|');
                    Equal(forms[1] + " ", c.CorrectCompletedWord(forms[0] + " "), "lets contraction case " + forms[0]);
                    Check(c.MayCorrectCompletedWord(forms[0] + " "), "lets reaches lean filter " + forms[0]);
                }

                Equal("let’s, ", c.CorrectCompletedWord("lets, "), "lets punctuation preserved");
                Equal(null, c.CorrectCompletedWord("Earlier lets "), "lets preceding text rejected");
                Equal(null, c.CorrectCompletedWord("let’s "), "lets already correct untouched");
                foreach (string input in new[]
                {
                    "id",
                    "Id"
                }

                )
                    foreach (char delimiter in new[]
                    {
                        ' ',
                        ',',
                        '!',
                        '\n',
                        '\t'
                    }

                    )
                        Equal("I’d" + delimiter, c.CorrectCompletedWord(input + delimiter), "explicit I'd contraction " + input);
                Equal(null, c.CorrectCompletedWord("ID "), "uppercase identifier preserved");
                Check(!c.MayCorrectCompletedWord("ID "), "uppercase identifier filter");
                foreach (string input in new[]
                {
                    "wont",
                    "Wont",
                    "WONT"
                }

                )
                    foreach (char delimiter in new[]
                    {
                        ' ',
                        ',',
                        '!',
                        '\n',
                        '\t'
                    }

                    )
                        Equal((input == "WONT" ? "WON’T" : input == "Wont" ? "Won’t" : "won’t") + delimiter, c.CorrectCompletedWord(input + delimiter), "explicit wont contraction " + input);
                foreach (string word in new[]
                {
                    "shed",
                    "shell",
                    "its",
                    "were",
                    "well",
                    "ill",
                    "hell",
                    "wed",
                    "an",
                    "age",
                    "since"
                }

                )
                    Equal(null, c.CorrectCompletedWord(word + " "), "ambiguous real English word preserved " + word);
                Equal("Doesn’t ", c.CorrectCompletedWord("Doesnt "), "capitalized contraction");
                Equal("DOESN’T ", c.CorrectCompletedWord("DOESNT "), "uppercase contraction");
                Equal(null, c.CorrectCompletedWord("doesn’t "), "already correct untouched");
                foreach (string input in new[]
                {
                    "earlier doesnt ",
                    "old\ndoesnt ",
                    "xdoesnt ",
                    "old-doesnt ",
                    "https://doesnt.com ",
                    "someone@doesnt.com ",
                    "C:\\doesnt ",
                    "doesnt>",
                    "doesnt",
                    "(earlier doesnt) ",
                    "old.doesnt "
                }

                )
                    Equal(null, c.CorrectCompletedWord(input), "unsafe or preceding text rejected " + input);
                Equal("\"doesn’t,\" ", c.CorrectCompletedWord("\"doesnt,\" "), "punctuation preserved");
                int entries = 0;
                string folder = Path.Combine(args[0], "data");
                byte[] data = File.ReadAllBytes(Path.Combine(folder, "english-corrections.dat"));
                using (BinaryReader reader = new BinaryReader(File.OpenRead(Path.Combine(folder, "english-corrections.idx"))))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        reader.ReadUInt64();
                        long offset = reader.ReadInt64();
                        int length = reader.ReadInt32();
                        string record = Encoding.UTF8.GetString(data, (int)offset, length);
                        int split = record.IndexOf('\0');
                        string key = record.Substring(0, split), replacement = record.Substring(split + 1);
                        entries++;
                        foreach (string prefix in new[]
                        {
                            "earlier doesnt is left exactly as typed. ",
                            "older teh words\r\n",
                            "😀 before\t",
                            ""
                        }

                        )
                            foreach (string punctuation in new[]
                            {
                                "",
                                ",",
                                "!",
                                ";"
                            }

                            )
                            {
                                InputState state = new InputState();
                                foreach (char ch in prefix + key + punctuation)
                                    state.Type(ch);
                                string candidate = state.CompletedEnglish(' ');
                                Equal(key + punctuation + " ", candidate, "whole current correction token " + key);
                                long generation = state.Generation;
                                state.Type(' ');
                                Check(state.Generation != generation, "delimiter generation " + key);
                                string output = c.CorrectCompletedWord(candidate);
                                Equal(replacement == key || new[] { "well", "were", "ill", "hell", "shell", "its", "wed", "shed" }.Contains(key) ? null : replacement + punctuation + " ", output, "entire correction dictionary " + key);
                                if (output != null)
                                {
                                    state.RecordReplacement(candidate, output, new IntPtr(7), true);
                                    Check(state.UndoOriginal == null && state.UndoOutput == null, "Backspace remains normal after automatic correction " + key);
                                }

                                Check(output == null || c.MayCorrectCompletedWord(candidate), "lean filter never drops correction " + key);
                                foreach (string wrapped in new[]
                                {
                                    "\"" + candidate.TrimEnd() + "\" ",
                                    "(“" + candidate.TrimEnd() + "”) ",
                                    candidate.ToUpperInvariant(),
                                    candidate.Replace('\'', '’')
                                }

                                )
                                    Check(c.CorrectCompletedWord(wrapped) == null || c.MayCorrectCompletedWord(wrapped), "lean filter preserves wrapping and case " + key);
                                int begin;
                                string suffix = " later doesnt stays untouched", document = prefix + candidate + suffix;
                                int caret = prefix.Length + candidate.Length;
                                Check(EditGuard.Locate(document, caret, caret, candidate, out begin) && begin == prefix.Length, "exact correction range " + key);
                                string result = document.Substring(0, begin) + (output ?? candidate) + document.Substring(caret);
                                Check(result.StartsWith(prefix, StringComparison.Ordinal) && result.EndsWith(suffix, StringComparison.Ordinal), "both sides unchanged " + key);
                                Check(!EditGuard.Locate(document, caret - 1, caret - 1, candidate, out begin), "moved caret never corrects earlier word " + key);
                            }
                    }
                }

                InputState overflowing = new InputState();
                foreach (char ch in new string ('x', 161) + "doesnt")
                    overflowing.Type(ch);
                Equal(null, overflowing.CompletedEnglish(' '), "overflow cannot correct trailing fragment");
                InputState reset = new InputState();
                foreach (char ch in "earlierdoesnt")
                    reset.Type(ch);
                reset.Clear();
                foreach (char ch in "doesnt")
                    reset.Type(ch);
                Equal("doesnt ", reset.CompletedEnglish(' '), "context reset discards older buffer");
                reset.Paused = true;
                Equal(null, reset.CompletedEnglish(' '), "pause blocks correction");
                for (int n = 0; n < 20000; n++)
                {
                    InputState state = new InputState();
                    foreach (char ch in "previous typo doesnt remains. doesnt")
                        state.Type(ch);
                    string input = state.CompletedEnglish(' ');
                    state.Type(' ');
                    long captured = state.Generation;
                    state.Type('x');
                    Check(state.Generation != captured, "new typing cancels pending correction " + n);
                    Equal("doesnt ", input, "never captures previous contraction " + n);
                    int begin;
                    string document = "previous typo doesnt remains. doesnt x";
                    Check(!EditGuard.Locate(document, document.Length, document.Length, input, out begin), "stale caret cannot delete preceding word " + n);
                }

                Console.WriteLine("Contraction/autocorrect dictionary entries checked: " + entries);
            }

            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            foreach (string error in errors)
                Console.WriteLine("FAIL " + error);
            return failed == 0 ? 0 : 1;
        }
    }
}
