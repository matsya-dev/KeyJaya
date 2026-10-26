// Noninteractive regressions: dialect aliases, token ambiguity, corpus samples, and generated phoneme sequences.
// Run: .\test.ps1 -Suite ItransTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace KeyJaya
{
    public static class ItransTests
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
                if (errors.Count < 50)
                    errors.Add(name);
            }
        }

        static void Equal(string expected, string actual, string name)
        {
            Check(expected == actual, name + " expected=" + expected + " actual=" + actual);
        }

        static string Canonical(string text)
        {
            return text.Normalize(NormalizationForm.FormC).Replace('ṁ', 'ṃ');
        }

        static string LegacyCanonical(string text)
        {
            return text.Normalize(NormalizationForm.FormC).Replace('ṃ', 'ṁ').Replace('Ṃ', 'Ṁ');
        }

        public static int Main(string[] args)
        {
            Stopwatch time = Stopwatch.StartNew();
            using (Core c = new Core(args[0]))
            {
                string[] tokens =
                {
                    "a",
                    "A",
                    "aa",
                    "i",
                    "I",
                    "ii",
                    "u",
                    "U",
                    "uu",
                    "R",
                    "Ri",
                    "RR",
                    "RRI",
                    "L",
                    "Li",
                    "LL",
                    "LLi",
                    "e",
                    "ai",
                    "o",
                    "au",
                    "ka",
                    "kha",
                    "ga",
                    "gha",
                    "~Na",
                    "N^a",
                    "ca",
                    "cha",
                    "Cha",
                    "CHa",
                    "ja",
                    "jha",
                    "~na",
                    "Ta",
                    "Tha",
                    "Da",
                    "Dha",
                    "Na",
                    "ta",
                    "tha",
                    "da",
                    "dha",
                    "na",
                    "pa",
                    "pha",
                    "ba",
                    "bha",
                    "ma",
                    "ya",
                    "ra",
                    "la",
                    "va",
                    "wa",
                    "sha",
                    "Sha",
                    "shha",
                    "sa",
                    "ha",
                    "M",
                    ".m",
                    "H",
                    ".a",
                    "kSha",
                    "x",
                    "j~na",
                    "GYa",
                    "tra",
                    "shra"
                };
                string[] unicode =
                {
                    "a",
                    "ā",
                    "ā",
                    "i",
                    "ī",
                    "ī",
                    "u",
                    "ū",
                    "ū",
                    "ṛ",
                    "ṛ",
                    "ṝ",
                    "ṝ",
                    "ḷ",
                    "ḷ",
                    "ḹ",
                    "ḷ",
                    "e",
                    "ai",
                    "o",
                    "au",
                    "ka",
                    "kha",
                    "ga",
                    "gha",
                    "ṅa",
                    "ṅa",
                    "ca",
                    "ca",
                    "cha",
                    "cha",
                    "ja",
                    "jha",
                    "ña",
                    "ṭa",
                    "ṭha",
                    "ḍa",
                    "ḍha",
                    "ṇa",
                    "ta",
                    "tha",
                    "da",
                    "dha",
                    "na",
                    "pa",
                    "pha",
                    "ba",
                    "bha",
                    "ma",
                    "ya",
                    "ra",
                    "la",
                    "va",
                    "va",
                    "śa",
                    "ṣa",
                    "ṣa",
                    "sa",
                    "ha",
                    "ṃ",
                    "ṃ",
                    "ḥ",
                    "’",
                    "kṣa",
                    "kṣa",
                    "jña",
                    "jña",
                    "tra",
                    "śra"
                };
                Equal(tokens.Length.ToString(), unicode.Length.ToString(), "guide table size");
                for (int i = 0; i < tokens.Length; i++)
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram",
                        "plain",
                        "itrans"
                    }

                    )
                        Equal(Core.Target(unicode[i], mode), c.ConvertItrans(tokens[i], mode), "guide " + tokens[i] + " to " + mode);
                const string indic = "āīūṛṝḷḹṅñṭḍṇśṣḥṁṃĀĪŪṚṜḶḸṄÑṬḌṆŚṢḤṀṂ";
                const string legacy = "äéüåèÿýìïöòëçñùààÄÉÜÅÈßÝÌÏÖÒËÇÑÙÀÀ";
                for (int i = 0; i < indic.Length; i++)
                {
                    Equal(legacy[i].ToString(), EncodingMap.Balaram(indic[i].ToString()), "IAST/Balaram chart " + i);
                    Equal(LegacyCanonical(indic[i].ToString()), EncodingMap.FromBalaram(legacy[i].ToString()), "Balaram/IAST chart " + i);
                    Equal(Core.Target(EncodingMap.FromBalaram(legacy[i].ToString()), "itrans"), c.ConvertBalaram(legacy[i].ToString(), "itrans"), "explicit Balaram/ITRANS chart " + i);
                }

                Equal("kṛṣṇa tvadīya-pada-paṅkaja-pañjarāntam viśatu prāṇa", c.ConvertItrans("kRShNa tvadIya-pada-pa~Nkaja-pa~njarAntam vishatu prANa", "unicode"), "full supplied vocabulary phrase");
                Equal("kṛṣṇa", c.ConvertItrans("KRShNa", "unicode"), "proper-name initial");
                Equal("\"kṛṣṇa,\"\r\n\r\n(śrī)\tprāṇa!", c.ConvertItrans("\"kRShNa,\"\r\n\r\n(shrI)\tprANa!", "unicode"), "punctuation and CRLF");
                foreach (string input in new[]
                {
                    "kRShNaQfail",
                    "ésh",
                    "kRShNaЖ",
                    "kRShNá",
                    "{",
                    "~bad",
                    "^bad"
                }

                )
                    Equal(input, c.ConvertItrans(input, "unicode"), "unsupported token preserved atomically " + input);
                string safe = "https://example.com/kRShNa person@shrI.com C:\\Users\\kRShNa\\file \\\\server\\shrI /* kRShNa */";
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain",
                    "itrans"
                }

                )
                {
                    Equal(safe, c.ConvertItrans(safe, mode), "literal protected " + mode);
                    Equal(safe, c.ConvertBalaram(safe, mode), "Balaram protected " + mode);
                }

                foreach (string input in new[]
                {
                    "an age since",
                    "The child changed chapters while chatting.",
                    "café naïve jalapeño",
                    "AN AGE SINCE",
                    "she's age’s since’s"
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
                        Equal(input, c.Convert(input, mode), "automatic English preservation " + mode);
                string[] samples =
                {
                    "kṛṣṇa",
                    "śrī",
                    "rāja",
                    "rājā",
                    "viśatu",
                    "aśaucam",
                    "vepathuḥ",
                    "tvadīya",
                    "pratiṣṭhā",
                    "prāṇa",
                    "saṁskṛta",
                    "caitanya",
                    "chāyā",
                    "jñāna",
                    "śāstra",
                    "ṣaṭ",
                    "ṛi",
                    "ṝī",
                    "śha",
                    "aṅga",
                    "kḷpta",
                    "ḹ",
                    "kṣa"
                };
                foreach (string word in samples)
                {
                    string it = Itrans.Encode(word), bal = EncodingMap.Balaram(word), plain = EncodingMap.Ascii(word), decoded;
                    Check(Itrans.TryWord(it, out decoded), "encoded word parses " + word);
                    Equal(Canonical(word), decoded, "ITRANS round trip " + word);
                    Equal(bal, c.ConvertItrans(it, "balaram"), "ITRANS/Balaram " + word);
                    Equal(plain, c.ConvertItrans(it, "plain"), "ITRANS/plain " + word);
                    Equal(LegacyCanonical(word), c.ConvertBalaram(bal, "unicode"), "Balaram/IAST " + word);
                    Equal(it, c.ConvertBalaram(EncodingMap.Balaram(word), "itrans"), "Balaram/ITRANS " + word);
                    Equal(plain, c.ConvertBalaram(bal, "plain"), "Balaram/plain " + word);
                    Equal(it, Itrans.Encode(word.Normalize(NormalizationForm.FormD)), "decomposed encoding " + word);
                }

                foreach (string name in new[]
                {
                    "kṛṣṇa",
                    "śrī",
                    "tvadīya",
                    "viśatu",
                    "rāja",
                    "pratiṣṭhā",
                    "vepathuḥ"
                }

                )
                {
                    Equal(Itrans.Encode(name), c.Convert(name, "itrans"), "copied IAST/ITRANS " + name);
                    Equal(EncodingMap.Balaram(name), c.Convert(name, "balaram"), "copied IAST/Balaram " + name);
                    Equal(name, c.Convert(EncodingMap.Balaram(name), "unicode"), "automatic copied Balaram/IAST " + name);
                }

                Equal("kRShNa", c.Convert("Krsna", "itrans"), "plain alias/ITRANS");
                Equal("an age since kRShNa", c.Convert("an age since Krsna", "itrans"), "mixed plain source");
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain",
                    "itrans"
                }

                )
                {
                    string english = "An age since café naïve jalapeño doesn't she's";
                    Equal(english, c.ConvertBalaram(english, mode), "English within explicit Balaram source " + mode);
                    Equal("An age since " + Core.Target("rāja", mode) + ".", c.ConvertBalaram("An age since räja.", mode), "mixed Balaram and English " + mode);
                }

                Random rng = new Random(20261011);
                int[] pool =
                {
                    0,
                    1,
                    3,
                    4,
                    6,
                    7,
                    9,
                    11,
                    13,
                    15,
                    17,
                    18,
                    19,
                    20,
                    21,
                    22,
                    23,
                    24,
                    25,
                    27,
                    29,
                    31,
                    32,
                    33,
                    34,
                    35,
                    36,
                    37,
                    38,
                    39,
                    40,
                    41,
                    42,
                    43,
                    44,
                    45,
                    46,
                    47,
                    48,
                    49,
                    50,
                    51,
                    52,
                    54,
                    55,
                    57,
                    58,
                    59,
                    61
                };
                for (int n = 0; n < 20000; n++)
                {
                    List<string> parts = new List<string>();
                    StringBuilder expected = new StringBuilder();
                    for (int j = 0; j < rng.Next(1, 16); j++)
                    {
                        int i = pool[rng.Next(pool.Length)];
                        parts.Add(tokens[i]);
                        expected.Append(unicode[i]);
                    }

                    string word = expected.ToString(), input = string.Join("{}", parts.ToArray());
                    Equal(word, c.ConvertItrans(input, "unicode"), "random phoneme decode " + n);
                    Equal(EncodingMap.Balaram(word), c.ConvertItrans(input, "balaram"), "random legacy " + n);
                    Equal(EncodingMap.Ascii(word), c.ConvertItrans(input, "plain"), "random plain " + n);
                    Equal(word, c.ConvertItrans(Itrans.Encode(word), "unicode"), "random encoded round trip " + n);
                }

                int corpus = 0;
                string dataPath = Path.Combine(args[0], "data");
                byte[] data = File.ReadAllBytes(Path.Combine(dataPath, "conversion.dat"));
                using (BinaryReader reader = new BinaryReader(File.OpenRead(Path.Combine(dataPath, "conversion.idx"))))
                {
                    int record = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        reader.ReadUInt64();
                        long offset = reader.ReadInt64();
                        int size = reader.ReadInt32();
                        record++;
                        string row = Encoding.UTF8.GetString(data, (int)offset, size), word = row.Substring(row.IndexOf('\0') + 1);
                        if (!Regex.IsMatch(word, @"^[a-zāīūṛṝḷḹṅñṭḍṇśṣḥṁṃ]+$"))
                            continue;
                        if (c.English(word) && !Core.IndicLoanwords.Contains(word))
                        {
                            Equal(word, c.ConvertBalaram(word, "itrans"), "English overlap remains English " + word);
                            continue;
                        }

                        corpus++;
                        string it = Itrans.Encode(word), roman = EncodingMap.Ascii(word) == "bhaktivedanta" ? "bhaktivedanta" : EncodingMap.Ascii(word) == "bhaktivedantas" ? "bhaktivedantas" : word == "mathurā" ? "Mathurā" : word, bal = EncodingMap.Balaram(roman);
                        if (EncodingMap.IsIndic(word) && !c.English(word))
                        {
                            Equal(roman, c.Convert(word, "unicode"), "corpus copied IAST/IAST " + word);
                            Equal(bal, c.Convert(word, "balaram"), "corpus copied IAST/Balaram " + word);
                            Equal(EncodingMap.Ascii(roman), c.Convert(word, "plain"), "corpus copied IAST/plain " + word);
                            Equal(Itrans.Encode(roman), c.Convert(word, "itrans"), "corpus copied IAST/ITRANS " + word);
                        }

                        Equal(Canonical(roman), c.ConvertItrans(it, "unicode"), "corpus ITRANS/IAST " + word);
                        Equal(bal, c.ConvertItrans(it, "balaram"), "corpus ITRANS/Balaram " + word);
                        Equal(EncodingMap.Ascii(roman), c.ConvertItrans(it, "plain"), "corpus ITRANS/plain " + word);
                        Equal(LegacyCanonical(roman), c.ConvertBalaram(EncodingMap.Balaram(word), "unicode"), "corpus Balaram/IAST " + word);
                        Equal(Itrans.Encode(roman), c.ConvertBalaram(EncodingMap.Balaram(word), "itrans"), "corpus Balaram/ITRANS " + word);
                    }
                }

                Console.WriteLine("Conversion corpus samples: " + corpus + "; random phoneme sequences: 20000");
                string[] raja = c.ClipboardChoices("rāja", "balaram", "auto");
                Check(raja.Length >= 2 && raja[0] == "räja" && raja[1] == "räjä", "requested raja preference and alternative");
                IntPtr owner = new IntPtr(42);
                int[] field =
                {
                    10,
                    20,
                    30
                };
                DateTime now = new DateTime(2026, 10, 11);
                ClipboardCycle cycle = new ClipboardCycle();
                string next;
                cycle.Start("räja", raja, "balaram", "auto", 100, owner, field, now);
                bool sawPlain = false;
                for (int step = 1; step <= raja.Length * 2; step++)
                {
                    Check(cycle.Next("balaram", "auto", step == 1 ? "räja" : "", (uint)(100 + step - 1), owner, field, now, out next), "repeated shortcut survives clipboard clearing");
                    Equal(raja[step % raja.Length], next, "all attested choices cycle and wrap after clipboard clearing");
                    sawPlain |= next == "raja";
                    cycle.Commit(next, (uint)(100 + step), now);
                    cycle.Released((uint)(100 + step));
                }

                Check(sawPlain, "original plain word remains in expanded cycle");
                for (int n = 0; n < 10000; n++)
                {
                    cycle.Start("räja", raja, "balaram", "auto", 100, owner, field, now);
                    Check(!cycle.Next("balaram", "auto", "räja", 101, owner, field, now, out next), "new identical copy cancels " + n);
                    cycle.Start("räja", raja, "balaram", "auto", 100, owner, field, now);
                    Check(!cycle.Next("balaram", "auto", "räja", 100, owner, new[] { 10, 20, 31 }, now, out next), "another field cancels " + n);
                    cycle.Start("räja", raja, "balaram", "auto", 100, owner, field, now);
                    Check(!cycle.Next("unicode", "auto", "räja", 100, owner, field, now, out next), "different output resets " + n);
                    cycle.Start("räja", raja, "balaram", "auto", 100, owner, field, now);
                    Check(!cycle.Next("balaram", "auto", "räja", 100, owner, field, now.AddSeconds(11), out next), "expiry cancels " + n);
                    Check(!ClipboardCycle.MayClearClipboard(100, 101, true, true), "new clipboard never erased " + n);
                    Check(!ClipboardCycle.MayClearClipboard(100, 100, false, true), "unverified paste never erased " + n);
                    Check(!ClipboardCycle.MayClearClipboard(100, 100, true, false), "changed field never erased " + n);
                    Check(ClipboardCycle.MayClearClipboard(100, 100, true, true), "verified owned clipboard may clear " + n);
                }

                int words = 0;
                foreach (string row in File.ReadLines(Path.Combine(dataPath, "english-guard.txt")))
                {
                    if (Core.IndicLoanwords.Contains(row))
                        continue;
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram",
                        "plain",
                        "itrans"
                    }

                    )
                        Check(c.ClipboardChoices(row, mode, "auto").Length == 0, "English has no clipboard cycle " + row + " " + mode);
                    words++;
                }

                Console.WriteLine("English words checked for clipboard cycling: " + words);
                int cyclingWords = 0;
                foreach (string word in new[]
                {
                    "rāja",
                    "gāna",
                    "kṛṣṇa",
                    "śrī",
                    "prāṇa",
                    "tvadīya",
                    "viśatu",
                    "pratiṣṭhā",
                    "vepathuḥ",
                    "aśaucam",
                    "jñāna",
                    "bhakti",
                    "gaura",
                    "rāma",
                    "hari",
                    "śakti",
                    "ātman",
                    "gopīnātha",
                    "mādhava",
                    "vṛndāvana"
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
                    {
                        string[] options = c.ClipboardChoices(word, mode, "auto");
                        if (options.Length < 2)
                            continue;
                        cyclingWords++;
                        cycle.Start(options[0], options, mode, "auto", 100, owner, field, now);
                        for (int n = 1; n <= options.Length * 3; n++)
                        {
                            string previous = cycle.Current;
                            Check(cycle.Next(mode, "auto", n == 1 ? previous : "", (uint)(100 + n - 1), owner, field, now, out next), "repeat available " + word + " " + mode);
                            Equal(options[n % options.Length], next, "repeat spelling " + word + " " + mode);
                            cycle.Commit(next, (uint)(100 + n), now);
                            cycle.Released((uint)(100 + n));
                        }
                    }

                Console.WriteLine("Named word/format cycling combinations: " + cyclingWords);
            }

            Console.WriteLine("PASS: " + passed + " FAIL: " + failed + " TOTAL_MS: " + time.ElapsedMilliseconds);
            foreach (string error in errors)
                Console.WriteLine("FAIL " + error);
            return failed == 0 ? 0 : 1;
        }
    }
}
