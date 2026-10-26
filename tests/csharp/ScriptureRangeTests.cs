// Noninteractive regressions: record integrity, corpus ranges, paired translations, italics, and exact limits.
// Run: .\test.ps1 -Suite ScriptureRangeTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KeyJaya
{
    public static class ScriptureRangeTests
    {
        static long passed, failed;
        static void Check(bool value, string label)
        {
            if (value)
                passed++;
            else if (failed++ < 30)
                Console.WriteLine("FAIL " + label);
        }

        static void Equal(string expected, string actual, string label)
        {
            Check(expected == actual, label);
        }

        static HashSet<string> References(string root)
        {
            byte[] data = File.ReadAllBytes(Path.Combine(root, "data/scripture.dat"));
            var result = new HashSet<string>(StringComparer.Ordinal);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            ulong previousHash = 0;
            bool firstRecord = true;
            var numeric = new Regex(@"^(?:BG|SB|CC|BS|NOI|ISO)\d+(?:\.\d+){0,2}$");
            using (var reader = new BinaryReader(File.OpenRead(Path.Combine(root, "data/scripture.idx"))))
            {
                Check(reader.BaseStream.Length % 20 == 0, "scripture index has complete records");
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    ulong storedHash = reader.ReadUInt64();
                    long offset = reader.ReadInt64();
                    int length = reader.ReadInt32();
                    Check(firstRecord || storedHash >= previousHash, "scripture lookup hashes remain sorted");
                    previousHash = storedHash;
                    firstRecord = false;
                    bool bounded = offset >= 0 && length > 0 && length <= 4000000 && offset <= data.LongLength - length;
                    Check(bounded, "scripture index points inside unchanged data bounds");
                    if (!bounded)
                        continue;
                    string record = Encoding.UTF8.GetString(data, (int)offset, length);
                    string key = record.Substring(0, record.IndexOf('\0'));
                    Check(IndexedStore.Hash(key) == storedHash, "scripture record identifier agrees with its index hash");
                    Check(keys.Add(key), "different scripture payloads never share a record identifier");
                    if (!key.EndsWith(">", StringComparison.Ordinal))
                        continue;
                    key = key.Substring(0, key.Length - 1);
                    Match cb = Regex.Match(key, @"^CB(Adi|Madhya|Antya)(\d+)\.(\d+)$");
                    if (cb.Success)
                        key = "CB" + (cb.Groups[1].Value == "Adi" ? "1" : cb.Groups[1].Value == "Madhya" ? "2" : "3") + "." + int.Parse(cb.Groups[2].Value) + "." + int.Parse(cb.Groups[3].Value);
                    if (numeric.IsMatch(key) || cb.Success)
                        result.Add(key);
                }
            }

            return result;
        }

        static void RepairedIdentifiers(Core core, string root)
        {
            string fixture = Path.Combine(Path.GetDirectoryName(typeof(ScriptureRangeTests).Assembly.Location), "inputs", "scripture-identifiers.tsv");
            int checkedRecords = 0;
            using (var store = new IndexedStore(Path.Combine(root, "data"), "scripture"))
            using (var digest = SHA256.Create())
            {
                foreach (string row in File.ReadLines(fixture))
                {
                    if (row.StartsWith("#", StringComparison.Ordinal) || row.Length == 0)
                        continue;
                    string[] cells = row.Split('\t');
                    Check(cells.Length == 2, "identifier regression fixture has key and original payload hash");
                    if (cells.Length != 2)
                        continue;
                    string key = cells[0], value = store.Get(key);
                    Check(value != null, "repaired identifier is reachable through actual indexed lookup " + key);
                    if (value == null)
                        continue;
                    string hash = BitConverter.ToString(digest.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
                    Equal(cells[1], hash, "repair preserves original verse/translation payload " + key);
                    string reference = key.Substring(0, key.Length - 1), mode = key.EndsWith("<", StringComparison.Ordinal) ? "balaram" : key.EndsWith(":", StringComparison.Ordinal) ? "plain" : "unicode";
                    string output = core.CtrlExpansion(reference, mode);
                    Check(!string.IsNullOrEmpty(output), "repaired verse/translation works through Ctrl expansion " + key);
                    checkedRecords++;
                }
            }

            Check(checkedRecords == 276, "all 276 approved repairs are regression checked");
            Console.WriteLine("Repaired identifiers checked: " + checkedRecords + "; original text payload hashes preserved.");
        }

        static void Styles(Core core, string reference, string output, string mode, string[] verses)
        {
            ItalicSpan[] spans = core.CtrlItalics(reference, output, mode);
            var forced = new bool[output.Length];
            foreach (var span in spans.Where(s => s.Force))
            {
                Check(span.Start >= 0 && span.Length > 0 && span.Start + span.Length <= output.Length, "valid verse italic bounds " + reference);
                for (int n = span.Start; n < span.Start + span.Length; n++)
                    forced[n] = true;
            }

            var expected = new bool[output.Length];
            int after = 0;
            foreach (string verse in verses)
            {
                int begin = output.IndexOf(verse, after, StringComparison.Ordinal);
                Check(begin >= 0, "verse remains in expected order " + reference);
                if (begin < 0)
                    continue;
                for (int n = begin; n < begin + verse.Length; n++)
                    expected[n] = true;
                after = begin + verse.Length;
            }

            for (int n = 0; n < output.Length; n++)
            {
                if (char.IsLetter(output[n]))
                    Check(forced[n] == expected[n], "only complete verses have forced italics " + reference + " at " + n);
            }

            var merged = WordSelection.MergeVerseSpans(output, spans);
            Check(merged != null && RichClipboard.Html(output, merged) != null, "valid Word and rich-editor formatting " + reference);
            foreach (var span in merged.Where(s => s.Force))
                for (int n = span.Start; n < span.Start + span.Length; n++)
                    if (char.IsLetter(output[n]))
                        Check(expected[n], "Word merging never crosses translation " + reference);
            string markdown = MarkdownFormatting.Italicize(output, spans);
            Equal(output, markdown.Replace("*", ""), "Markdown formatting preserves text and pairing " + reference);
        }

        static void Corpus(Core core, string root)
        {
            var references = References(root);
            int pairs = 0, formatted = 0, shared = 0;
            var books = new HashSet<string>();
            var clock = Stopwatch.StartNew();
            foreach (string reference in references.OrderBy(x => x, StringComparer.Ordinal))
            {
                Match end = Regex.Match(reference, @"^(.*?)(\d+)$");
                string next = end.Groups[1].Value + (int.Parse(end.Groups[2].Value) + 1);
                if (!references.Contains(next))
                    continue;
                string range = reference + "–" + (int.Parse(end.Groups[2].Value) + 1);
                pairs++;
                books.Add(Regex.Match(reference, @"^[A-Z]+").Value);
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string a = core.CtrlExpansion(reference, mode), b = core.CtrlExpansion(next, mode);
                    if (a == null || b == null)
                    {
                        Equal(null, core.CtrlExpansion(range, mode), "unavailable verse rejects whole range " + range);
                        continue;
                    }

                    Equal(a + "\n\n" + b, core.CtrlExpansion(range, mode), "corpus verse-only default " + range + " " + mode);
                    string ta = core.CtrlExpansion(reference + "T", mode), tb = core.CtrlExpansion(next + "T", mode);
                    if (ta == null || tb == null)
                    {
                        Equal(null, core.CtrlExpansion(range + "VT", mode), "missing translation rejects whole pair " + range);
                        continue;
                    }

                    string expected = ta == tb ? a + "\n\n" + b + "\n\n" + ta : a + "\n\n" + ta + "\n\n" + b + "\n\n" + tb;
                    string output = core.CtrlExpansion(range + "VT", mode);
                    Equal(expected, output, "corpus paired translations " + range + " " + mode);
                    Equal(ta == tb ? ta : ta + "\n\n" + tb, core.CtrlExpansion(range + "T", mode), "corpus translation-only order " + range + " " + mode);
                    if (ta == tb)
                        shared++;
                    if (pairs % 97 == 0 && output != null)
                    {
                        Styles(core, range + "VT", output, mode, new[] { a, b });
                        formatted++;
                    }
                }
            }

            Check(pairs > 10000, "large corpus of adjacent verse ranges checked");
            Check(books.Count == 7, "every supported book included in range tests");
            Check(shared > 0 && formatted > 100, "shared translations and distributed style samples covered");
            Console.WriteLine("Corpus ranges: " + pairs + "; books: " + string.Join(", ", books.OrderBy(x => x)) + "; formatting cases: " + formatted + "; shared-translation cases: " + shared + "; seconds: " + clock.Elapsed.TotalSeconds.ToString("F2"));
        }

        static void WriteStore(string root, Dictionary<string, string> values)
        {
            var rows = new List<Tuple<ulong, long, int>>();
            using (var data = new FileStream(Path.Combine(root, "data/scripture.dat"), FileMode.Create, FileAccess.Write))
            {
                foreach (var pair in values)
                {
                    byte[] record = Encoding.UTF8.GetBytes(pair.Key + "\0" + pair.Value);
                    rows.Add(Tuple.Create(IndexedStore.Hash(pair.Key), data.Position, record.Length));
                    data.Write(record, 0, record.Length);
                }
            }

            using (var index = new BinaryWriter(File.Create(Path.Combine(root, "data/scripture.idx"))))
                foreach (var row in rows.OrderBy(x => x.Item1))
                {
                    index.Write(row.Item1);
                    index.Write(row.Item2);
                    index.Write(row.Item3);
                }
        }

        static void Synthetic(string source)
        {
            string root = Path.Combine(Path.GetDirectoryName(typeof(ScriptureRangeTests).Assembly.Location), "scripture-range-artifacts", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "data"));
            foreach (string file in Directory.GetFiles(Path.Combine(source, "data")))
                if (!Path.GetFileName(file).StartsWith("scripture.", StringComparison.Ordinal))
                    File.Copy(file, Path.Combine(root, "data", Path.GetFileName(file)));
            Directory.CreateDirectory(Configuration.Folder(root));
            Configuration.Write(Configuration.Short(root), new Dictionary<string, string>());
            var records = new Dictionary<string, string>();
            foreach (string suffix in new[]
            {
                ">",
                "<",
                ":"
            }

            )
            {
                for (int n = 1; n <= 200; n++)
                {
                    records["BG90." + n + suffix] = "verse " + n;
                    records["BG90." + n + "T" + suffix] = "Translation " + n;
                }

                records["BG91.1" + suffix] = new string ('a', 200000);
                records["BG91.2" + suffix] = new string ('a', 200001);
                records["BG91.1T" + suffix] = "Translation";
                records["BG92.1" + suffix] = new string ('a', 99999);
                records["BG92.2" + suffix] = new string ('b', 99999);
                records["BG93.1" + suffix] = new string ('a', 100000);
                records["BG93.2" + suffix] = new string ('b', 99999);
                records["BG94.1" + suffix] = "first verse";
                records["BG94.2T" + suffix] = "Only translation";
                foreach (int n in new[]
                {
                    1,
                    3
                }

                )
                {
                    records["BG95." + n + suffix] = "verse " + n;
                    records["BG95." + n + "T" + suffix] = "Translation " + n;
                }

                for (int n = 1; n <= 4; n++)
                {
                    records["BG96." + n + suffix] = "verse " + n;
                    records["BG96." + n + "T" + suffix] = n == 3 ? "Separate translation" : "Shared translation";
                }
            }

            WriteStore(root, records);
            using (var core = new Core(root))
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string full = core.CtrlExpansion("BG90.1–200", mode);
                    Equal(string.Join("\n\n", Enumerable.Range(1, 200).Select(n => "verse " + n)), full, "exact 200-verse limit " + mode);
                    Equal(null, core.CtrlExpansion("BG90.1–201", mode), "201 verses rejected " + mode);
                    Equal(core.CtrlExpansion("BG90.1", mode), core.CtrlExpansion("BG90.1–1", mode), "one-verse range " + mode);
                    Check(core.CtrlExpansion("BG91.1", mode).Length == 200000, "exact output size limit " + mode);
                    Equal(null, core.CtrlExpansion("BG91.2", mode), "oversized single verse rejected " + mode);
                    Equal(null, core.CtrlExpansion("BG91.1VT", mode), "translation included in total size cap " + mode);
                    Equal(null, core.CtrlExpansion("BG91.1R", mode), "reference included in total size cap " + mode);
                    Check(core.CtrlExpansion("BG92.1–2", mode).Length == 200000, "range separators included in exact size cap " + mode);
                    Equal(null, core.CtrlExpansion("BG93.1–2", mode), "range overflow rejected before insertion " + mode);
                    Equal("first verse", core.CtrlExpansion("BG94.1", mode), "verse-only does not require translation " + mode);
                    Equal("Only translation", core.CtrlExpansion("BG94.2T", mode), "translation-only does not require verse " + mode);
                    foreach (string reference in new[]
                    {
                        "BG94.1VT",
                        "BG94.2VT",
                        "BG94.1–2",
                        "BG94.1–2T",
                        "BG95.1–3",
                        "BG95.1–3T",
                        "BG95.1–3VT"
                    }

                    )
                        Equal(null, core.CtrlExpansion(reference, mode), "missing first/middle/last record is atomic " + reference + " " + mode);
                    Equal("verse 1\n\nverse 2\n\nShared translation\n\nverse 3\n\nSeparate translation\n\nverse 4\n\nShared translation", core.CtrlExpansion("BG96.1–4VT", mode), "only adjacent shared translations grouped " + mode);
                }
        }

        static void UndoOrigins(Core core)
        {
            var owner = new IntPtr(73);
            int[] field =
            {
                1,
                9
            };
            foreach (string mode in new[]
            {
                "unicode",
                "balaram"
            }

            )
                foreach (string input in new[]
                {
                    "Krishna",
                    "Krsna",
                    "Rama",
                    "raja",
                    "Rama’s"
                }

                )
                {
                    string[] choices = core.CaretChoices(input, mode);
                    Check(choices.Length > 1, "undo case has cycle choices " + input);
                    if (choices.Length < 2)
                        continue;
                    var cycle = new CaretWordCycle();
                    cycle.Start(choices[0], choices, mode, owner, field, input);
                    string current = choices[0];
                    for (int n = 0; n < choices.Length * 100; n++)
                    {
                        string next;
                        Check(cycle.Next(current, mode, owner, field, out next), "cycle remains continuous " + input);
                        cycle.Commit(next);
                        current = next;
                        Equal(input, cycle.Original, "Backspace origin survives every cycle including plain " + input);
                    }

                    string prefix = "Earlier untouched ", suffix = ", later unchanged.";
                    string edited = prefix + current + suffix;
                    Equal(prefix + input + suffix, SelectionFormatting.Apply(edited, new[] { new TextPatch(prefix.Length, current, cycle.Original) }), "undo origin restores exact spelling and neighbors " + input);
                    cycle.Clear();
                    Equal(null, cycle.Original, "navigation clears previous undo ownership");
                }
        }

        public static int Main(string[] args)
        {
            using (var core = new Core(args[0]))
            {
                RepairedIdentifiers(core, args[0]);
                Corpus(core, args[0]);
                UndoOrigins(core);
            }

            Synthetic(args[0]);
            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
