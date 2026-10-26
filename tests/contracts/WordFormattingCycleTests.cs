// Native formatting evidence and undo decisions run against fake Word ranges.
// No Word instance, resident startup, clipboard or desktop input is used.
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace KeyJaya
{
    internal static class WordFormattingCycleTests
    {
        sealed class Font
        {
            public string Name { get; set; }
            public int Italic { get; set; }
            public int Bold { get; set; }
            public double Size { get; set; }

            public Font Duplicate
            {
                get
                {
                    return Copy();
                }
            }

            public Font Copy()
            {
                return (Font)MemberwiseClone();
            }

            public string Key
            {
                get
                {
                    return Name + ":" + Italic + ":" + Bold + ":" + Size;
                }
            }
        }

        sealed class Document
        {
            public readonly Font[] Fonts;
            public readonly int Start;
            public int Reads;
            public Document(int start, int length)
            {
                Start = start;
                Fonts = Enumerable.Range(0, length).Select(n => new Font { Name = n % 2 == 0 ? "Font A" : "Font B", Italic = n % 3 == 0 ? -1 : 0, Bold = n % 2 == 0 ? -1 : 0, Size = 10.5 + n % 4 }).ToArray();
            }

            public Scope Range(int begin, int end)
            {
                Reads++;
                if (begin < Start || end > Start + Fonts.Length || end < begin)
                    throw new InvalidOperationException("Native bounds changed");
                return new Scope(this, begin, end);
            }
        }

        sealed class Scope
        {
            readonly Document document;
            int start, end;
            public Scope(Document owner, int begin, int stop)
            {
                document = owner;
                start = begin;
                end = stop;
            }

            public int StoryType
            {
                get
                {
                    return 1;
                }
            }

            public Scope Duplicate
            {
                get
                {
                    return new Scope(document, start, end);
                }
            }

            public void SetRange(int begin, int stop)
            {
                start = begin;
                end = stop;
            }

            public Font Font
            {
                get
                {
                    return document.Fonts[start - document.Start];
                }

                set
                {
                    for (int n = start; n < end; n++)
                        document.Fonts[n - document.Start] = value.Copy();
                }
            }

            public string Text
            {
                get
                {
                    throw new Exception("Unexpected repeated text read");
                }
            }

            public object Revisions
            {
                get
                {
                    throw new Exception("Unexpected repeated revision scan");
                }
            }
        }

        static void Check(bool result, string label, ref int passed)
        {
            if (!result)
                throw new Exception("Word formatting cycle: " + label);
            passed++;
        }

        public static int Run()
        {
            int passed = 0;
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            const BindingFlags methods = BindingFlags.Static | BindingFlags.NonPublic;
            var capture = typeof(WordSelection).GetMethod("CaptureFonts", methods);
            var restore = typeof(WordSelection).GetMethod("RestoreFonts", methods);
            foreach (string word in new[]
            {
                "dharma",
                "dharma ",
                " (dharma), ",
                "\tbhakti\r\n",
                new string (' ', 314) + "dharma "
            }

            )
            {
                var document = new Document(31, word.Length);
                string[] original = document.Fonts.Select(font => font.Key).ToArray();
                var scope = document.Range(31, 31 + word.Length);
                var map = WordSelection.VisibleMap.Create(word, 31, 31 + word.Length, new int[0], new int[0]);
                using (var snapshot = (WordSelection.FontSnapshot)capture.Invoke(null, new object[] { document, scope, 31, word, map }))
                {
                    Check(snapshot != null && snapshot.Fonts.Count == word.Length && snapshot.Matches(document, 31, 1, word), "every accepted one-word selection retains original fonts, including surrounding whitespace", ref passed);
                    for (int n = 0; n < document.Fonts.Length; n++)
                        document.Fonts[n] = new Font
                        {
                            Name = "Converted",
                            Italic = -1,
                            Bold = 0,
                            Size = 42
                        };
                    restore.Invoke(null, new object[] { snapshot, scope, 31, (Func<bool>)(() => true), map });
                    Check(document.Fonts.Select(font => font.Key).SequenceEqual(original), "original names, italic, bold and sizes are restored for every character", ref passed);
                }
            }

            foreach (string refused in new[]
            {
                "two words",
                new string ('a', 322),
                ""
            }

            )
            {
                var document = new Document(0, refused.Length);
                var result = capture.Invoke(null, new object[] { document, null, 0, refused, null });
                Check(result == null && document.Reads == 0, "passages and oversized selections remain bounded before provider access", ref passed);
            }

            var cycle = new CaretWordCycle();
            cycle.Start("dharma", new[] { "dharma", "dharma" }, "unicode", new IntPtr(42), new[] { 1, 2 }, "dharma", 1);
            var owned = new Document(0, 6);
            var fonts = new WordSelection.FontSnapshot(owned, 0, 1, "dharma");
            var resident = (Resident)FormatterServices.GetUninitializedObject(typeof(Resident));
            GC.SuppressFinalize(resident);
            using (var timer = new System.Windows.Forms.Timer())
            {
                typeof(Resident).GetField("sync", instance).SetValue(resident, new object ());
                typeof(Resident).GetField("caretWords", instance).SetValue(resident, cycle);
                typeof(Resident).GetField("fontExpiry", instance).SetValue(resident, timer);
                typeof(Resident).GetField("wordUndoFonts", instance).SetValue(resident, fonts);
                typeof(Resident).GetField("expiry", instance).SetValue(resident, DateTime.UtcNow.AddSeconds(-30));
                var expire = typeof(Resident).GetMethod("ExpireWordUndoFonts", instance);
                expire.Invoke(resident, new object[] { DateTime.UtcNow });
                string next;
                Check(fonts.Matches(owned, 0, 1, "dharma") && cycle.Next("dharma", "unicode", new IntPtr(42), new[] { 1, 2 }, out next) && cycle.NextIsOriginal, "pausing beyond Backspace expiry keeps evidence for the active Ctrl cycle", ref passed);
                cycle.Clear();
                expire.Invoke(resident, new object[] { DateTime.UtcNow });
                Check(typeof(Resident).GetField("wordUndoFonts", instance).GetValue(resident) == null && !fonts.Matches(owned, 0, 1, "dharma"), "retired cycles release original-formatting ownership", ref passed);
            }

            fonts.Dispose();
            bool undo = CaretWordCycle.NeedsUndo("bhakti", "bhakti", false, false, false, false, true);
            var state = new InputState();
            state.RecordReplacement("bhakti", "bhakti", new IntPtr(42), !undo);
            Check(undo && state.UndoOriginal == "bhakti" && state.UndoOutput == "bhakti", "unchanged rich-editor conversions arm the real Backspace undo state", ref passed);
            Check(!CaretWordCycle.NeedsUndo("bhakti", "bhakti", false, false, false, false, false), "an untouched word keeps ordinary Backspace", ref passed);
            Check(!CaretWordCycle.NeedsUndo("bhakti", "bhaktī", true, false, false, false, true), "selected-word conversions retain Ctrl+Z rather than caret Backspace", ref passed);
            Check(!CaretWordCycle.NeedsUndo("SP", "Śrīla Prabhupāda", false, true, false, false, true), "short expansions keep their established undo policy", ref passed);
            Check(CaretWordCycle.NeedsUndo("bhakti", "bhakti", false, false, true, false, false) && CaretWordCycle.NeedsUndo("bhakti", "bhaktī", false, false, false, false, false) && CaretWordCycle.NeedsUndo("bhakti", "bhakti", false, false, false, true, false), "repeated, text-changing and native-font conversions retain manual undo", ref passed);
            foreach (bool originalItalic in new[]
            {
                false,
                true
            }

            )
            {
                string html = Encoding.UTF8.GetString(RichClipboard.RestoreItalics("bhakti & <test>", originalItalic));
                Check(html.Contains(originalItalic ? "<i>bhakti &amp; &lt;test&gt;</i>" : "font-style:normal\">bhakti &amp; &lt;test&gt;</span>"), "rich undo restores saved italic state with escaped original text", ref passed);
            }

            Console.WriteLine("Word formatting cycle: " + passed + " assertions; padded selections, delayed cycles, formatting-only Backspace and rich restoration.");
            return passed;
        }
    }
}
