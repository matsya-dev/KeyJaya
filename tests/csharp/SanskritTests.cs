// Noninteractive regressions: large corpus recognition with explicit English-collision exclusions.
// Run: .\test.ps1 -Suite SanskritTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

namespace KeyJaya
{
    public static class SanskritTests
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
                if (errors.Count < 60)
                    errors.Add(name);
            }
        }

        static void Equal(string expected, string actual, string name)
        {
            Check(expected == actual, name + " expected=" + expected + " actual=" + actual);
        }

        public static int Main(string[] args)
        {
            Stopwatch clock = Stopwatch.StartNew();
            using (Core c = new Core(args[0]))
            {
                string[, ] overlaps =
                {
                    {
                        "kshatriya",
                        "kṣatriya"
                    },
                    {
                        "lakshmi",
                        "lakṣmī"
                    },
                    {
                        "mandala",
                        "maṇḍala"
                    },
                    {
                        "mathura",
                        "Mathurā"
                    },
                    {
                        "godavari",
                        "godāvarī"
                    },
                    {
                        "varanasi",
                        "vārāṇasī"
                    },
                    {
                        "rishi",
                        "ṛṣi"
                    },
                    {
                        "saraswati",
                        "sarasvatī"
                    },
                    {
                        "ganesh",
                        "gaṇeśa"
                    },
                    {
                        "sannyasin",
                        "sannyāsin"
                    }
                };
                for (int n = 0; n < overlaps.GetLength(0); n++)
                {
                    Equal(overlaps[n, 1], c.Trigger(overlaps[n, 0] + ">"), "Explicit Indic loanword Unicode " + n);
                    Equal(EncodingMap.Balaram(overlaps[n, 1]), c.Trigger(overlaps[n, 0] + "<"), "Explicit Indic loanword Balaram " + n);
                    if (c.English(overlaps[n, 0]) && !Core.IndicLoanwords.Contains(overlaps[n, 0]))
                        Equal(overlaps[n, 0], c.ConvertCopied(overlaps[n, 0], "unicode"), "English prose overlap remains protected " + n);
                }

                foreach (string term in Core.ExplicitIndicTriggers)
                    foreach (char suffix in new[]
                    {
                        '>',
                        '<'
                    }

                    )
                        Check(c.Trigger(term + suffix) != null, "Recognized explicit term resolves " + term + suffix);
                foreach (string term in new[]
                {
                    "an",
                    "age",
                    "since",
                    "this",
                    "well"
                }

                )
                    foreach (char suffix in new[]
                    {
                        '>',
                        '<'
                    }

                    )
                        Equal(null, c.Trigger(term + suffix), "Ordinary English suffix stays protected " + term + suffix);
                string[] inputs =
                {
                    "pratistha",
                    "asaucyam",
                    "prana",
                    "vepathuh",
                    "tvadiya",
                    "visatu"
                };
                string[] outputs =
                {
                    "pratiṣṭhā",
                    "aśaucam",
                    "prāṇa",
                    "vepathuḥ",
                    "tvadīya",
                    "viśatu"
                };
                for (int i = 0; i < inputs.Length; i++)
                {
                    Equal(outputs[i], c.Trigger(inputs[i] + ">"), "requested Unicode " + inputs[i]);
                    Equal(EncodingMap.Balaram(outputs[i]), c.Trigger(inputs[i] + "<"), "requested Balaram " + inputs[i]);
                    Equal(outputs[i], c.Trigger(EncodingMap.Balaram(outputs[i]) + ">"), "requested Balaram input " + inputs[i]);
                    Equal(EncodingMap.Balaram(outputs[i]), c.Trigger(EncodingMap.Balaram(outputs[i]) + "<"), "already Balaram removes suffix " + inputs[i]);
                }

                Equal("tvadīya", c.Trigger("tvadīya>"), "already Unicode removes suffix");
                Equal("prana is mentioned here", c.Convert("prana is mentioned here", "unicode"), "explicit suffix does not disable prose English guard");
                Equal("tvadīya-pada-paṅkaja-pañjarāntam", c.Trigger("tvadiya-pada-pankaja-panjarantam>"), "attested compound trigger");
                Equal("an age since", c.Convert("an age since", "unicode"), "English regression after expansion");
                InputState token = new InputState();
                string captured = null;
                foreach (char x in "earlier words tvadiya-pada-pankaja-panjarantam>")
                {
                    string next = token.Type(x);
                    if (next != null)
                        captured = next;
                }

                Equal("tvadiya-pada-pankaja-panjarantam>", captured, "whole compound current token");
                int eligible = 0, protectedWords = 0, longWords = 0;
                int longest = 0;
                string folder = Path.Combine(args[0], "data");
                byte[] data = File.ReadAllBytes(Path.Combine(folder, "conversion.dat"));
                using (BinaryReader reader = new BinaryReader(File.OpenRead(Path.Combine(folder, "conversion.idx"))))
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        reader.ReadUInt64();
                        long offset = reader.ReadInt64();
                        int size = reader.ReadInt32();
                        string entry = Encoding.UTF8.GetString(data, (int)offset, size);
                        int split = entry.IndexOf('\0');
                        string key = entry.Substring(0, split);
                        longest = Math.Max(longest, key.Length);
                        if (!Regex.IsMatch(key, @"^[a-z]+(?:-[a-z]+)*$"))
                            continue;
                        if (c.English(key) && !Core.IndicLoanwords.Contains(key) && !Core.ExplicitIndicTriggers.Contains(key))
                        {
                            protectedWords++;
                            continue;
                        }

                        if (key.Length > 160)
                        {
                            longWords++;
                            continue;
                        }

                        eligible++;
                        foreach (char suffix in new[]
                        {
                            '>',
                            '<'
                        }

                        )
                        {
                            string actual = c.Trigger(key + suffix);
                            Check(actual != null, "corpus entry must resolve " + key + suffix);
                            if (actual != null)
                            {
                                InputState state = new InputState();
                                string candidate = null;
                                foreach (char ch in "English before. " + key + suffix)
                                {
                                    string x = state.Type(ch);
                                    if (x != null)
                                        candidate = x;
                                }

                                Equal(key + suffix, candidate, "full corpus current word " + key);
                            }
                        }
                    }
                }

                Console.WriteLine("Corpus eligible ASCII word/compound aliases: " + eligible + " protected collisions: " + protectedWords + " over limit: " + longWords + " maximum key length: " + longest);
                // A separate English prose corpus was never used to generate the guard.
                string book = File.ReadAllText(args[1], Encoding.UTF8);
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string output = c.Convert(book, mode);
                    Check(output == book, "holdout novel unchanged " + mode);
                    if (output != book)
                    {
                        int i = 0;
                        while (i < book.Length && i < output.Length && book[i] == output[i])
                            i++;
                        errors.Add("holdout difference " + mode + " offset " + i + " input=" + book.Substring(Math.Max(0, i - 30), Math.Min(110, book.Length - Math.Max(0, i - 30))) + " output=" + output.Substring(Math.Max(0, i - 30), Math.Min(110, output.Length - Math.Max(0, i - 30))));
                    }
                }

                Console.WriteLine("Holdout English corpus characters: " + book.Length);
            }

            Console.WriteLine("PASS: " + passed + " FAIL: " + failed + " TOTAL_MS: " + clock.ElapsedMilliseconds);
            foreach (string error in errors)
                Console.WriteLine("FAIL " + error);
            return failed == 0 ? 0 : 1;
        }
    }
}
