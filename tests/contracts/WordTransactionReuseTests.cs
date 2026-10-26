// Exercise the production replay/snapshot paths with a fake Word range.
// Provider calls are counted; unexpected text/revision reads fail immediately.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KeyJaya
{
    internal static class WordTransactionReuseTests
    {
        sealed class Calls
        {
            public int Duplicates, Selections, Fonts, Italics, TailStart, TailEnd, StyleWrites;
            public int Italic = 0;
            public int Bold = 0;
            public bool FailFont;
            public readonly List<int> Restored = new List<int>();
            public void Reset()
            {
                Duplicates = Selections = Fonts = Italics = 0;
                StyleWrites = 0;
                Restored.Clear();
            }
        }

        sealed class Font
        {
            readonly Calls calls;
            public Font(Calls value)
            {
                calls = value;
            }

            public int Bold
            {
                get
                {
                    return calls.Bold;
                }
            }

            public int Italic
            {
                get
                {
                    calls.Italics++;
                    return calls.Italic;
                }

                set
                {
                    calls.StyleWrites++;
                    calls.Italic = value;
                }
            }
        }

        sealed class Scope
        {
            readonly Calls calls;
            int start;
            public Scope(Calls value)
            {
                calls = value;
            }

            public Scope Duplicate
            {
                get
                {
                    calls.Duplicates++;
                    return new Scope(calls);
                }
            }

            public void SetRange(int begin, int end)
            {
                calls.Selections++;
                start = begin;
                calls.TailStart = begin;
                calls.TailEnd = end;
            }

            public Font Font
            {
                get
                {
                    calls.Fonts++;
                    if (calls.FailFont)
                        throw new InvalidOperationException("Provider unavailable");
                    return new Font(calls);
                }

                set
                {
                    calls.Restored.Add(start);
                }
            }

            public string Text
            {
                get
                {
                    throw new Exception("Repeated native text read");
                }
            }

            public object Revisions
            {
                get
                {
                    throw new Exception("Repeated revision scan");
                }
            }
        }

        public sealed class Record
        {
            public int Ends;
            public int CustomRecordLevel
            {
                get
                {
                    return 1;
                }
            }

            public void EndCustomRecord()
            {
                Ends++;
            }
        }

        static void Check(bool result, string message, ref int passed)
        {
            if (!result)
                throw new Exception("Word transaction reuse: " + message);
            passed++;
        }

        public static int Run()
        {
            int passed = 0;
            object document = new object ();
            var calls = new Calls();
            var scope = new Scope(calls);
            var map = WordSelection.VisibleMap.Create("bhakti", 19, 25, new int[0], new int[0]);
            var anchor = WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, map, 1);
            Check(anchor != null && calls.Duplicates == 1 && calls.Selections == 1 && calls.Fonts == 1 && calls.Italics == 1, "capture needs only a tail style read, without another text/revision scan", ref passed);
            Check(calls.TailStart == 24 && calls.TailEnd == 25, "continuation comes from the owned final character", ref passed);
            var record = new Record();
            Check(!anchor.DeferRecord(new object (), 19, 1, map, record), "other document cannot own the undo record", ref passed);
            Check(!anchor.DeferRecord(document, 18, 1, map, record) && !anchor.DeferRecord(document, 19, 2, map, record), "range and story ownership remain exact", ref passed);
            Check(anchor.DeferRecord(document, 19, 1, map, record), "matching native transaction can transfer its record", ref passed);
            Check(!anchor.DeferRecord(document, 19, 1, map, record), "record transfers only once", ref passed);
            Check(anchor.FinishRecord() && anchor.FinishRecord() && record.Ends == 1, "one-step undo record finishes exactly once", ref passed);
            using (var fonts = new WordSelection.FontSnapshot(document, 19, 1, "bhakti"))
            {
                fonts.ContinuationItalic = -1;
                calls.Reset();
                anchor = WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, map, 1, fonts);
                Check(anchor != null && calls.Duplicates == 0 && calls.Fonts == 0 && calls.Italics == 0, "owned font snapshot avoids all extra native style calls", ref passed);
                var field = typeof(WordSelection.ReplayAnchor).GetField("continuationItalic", BindingFlags.Instance | BindingFlags.NonPublic);
                Check((int)field.GetValue(anchor) == -1, "original continuation survives an italic conversion cycle", ref passed);
                foreach (var changed in new[]
                {
                    new object[]
                    {
                        new object (),
                        19,
                        1
                    },
                    new object[]
                    {
                        document,
                        20,
                        1
                    },
                    new object[]
                    {
                        document,
                        19,
                        2
                    }
                }

                )
                {
                    calls.Reset();
                    int begin = (int)changed[1];
                    var moved = WordSelection.VisibleMap.Create("bhakti", begin, begin + 6, new int[0], new int[0]);
                    anchor = WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", changed[0], scope, moved, (int)changed[2], fonts);
                    Check(anchor != null && calls.Italics == 1 && (int)field.GetValue(anchor) == 0, "a stale document/range/story snapshot forces a fresh tail read", ref passed);
                }

                fonts.Retire();
                calls.Reset();
                Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, map, 1, fonts) != null && calls.Italics == 1, "retired snapshots do not supply continuation style", ref passed);
            }

            calls.Reset();
            var tracked = WordSelection.VisibleMap.Create("rāaja", 10, 15, new[] { 12 }, new[] { 13 });
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "rāja", document, scope, tracked, 1) != null && calls.TailStart == 14 && calls.TailEnd == 15, "tracked deletions retain native continuation coordinates", ref passed);
            var terminal = WordSelection.VisibleMap.Create("bhakti\n", 19, 26, new int[0], new int[0]);
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, terminal, 1) != null && calls.TailStart == 24 && calls.TailEnd == 25, "paragraph marker does not supply continuation formatting", ref passed);
            calls.Reset();
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "changed", document, scope, map, 1) == null && calls.Duplicates == 0, "mismatched expected text is rejected before provider work", ref passed);
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, map, 5) == null && calls.Duplicates == 0, "unsupported stories stay excluded", ref passed);
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, null, 1) == null && calls.Duplicates == 0, "missing projection is never a blank caret", ref passed);
            calls.FailFont = true;
            Check(WordSelection.ReplayAnchor.FromVerifiedSelection(new IntPtr(42), "bhakti", document, scope, map, 1) == null, "provider failures keep replay unavailable", ref passed);
            calls.FailFont = false;
            // Restoration uses the transaction's already-verified result map,
            // retaining per-character fonts and skipping tracked deletions.
            using (var fonts = new WordSelection.FontSnapshot(document, 10, 1, "rāja"))
            {
                for (int n = 0; n < 4; n++)
                    fonts.Fonts.Add(new Font(calls));
                calls.Reset();
                var restore = typeof(WordSelection).GetMethod("RestoreFonts", BindingFlags.Static | BindingFlags.NonPublic);
                restore.Invoke(null, new object[] { fonts, scope, 10, (Func<bool>)(() => true), tracked });
                Check(string.Join(",", calls.Restored) == "10,11,13,14", "font restoration preserves each visible character without another revision scan", ref passed);
                calls.Reset();
                bool cancelled = false;
                try
                {
                    restore.Invoke(null, new object[] { fonts, scope, 10, (Func<bool>)(() => false), tracked });
                }
                catch (TargetInvocationException error)
                {
                    cancelled = error.InnerException is InvalidOperationException;
                }

                Check(cancelled && calls.Restored.Count == 0, "lost context rejects restoration before a font write", ref passed);
                calls.Reset();
                bool moved = false;
                try
                {
                    restore.Invoke(null, new object[] { fonts, scope, 9, (Func<bool>)(() => true), tracked });
                }
                catch (TargetInvocationException error)
                {
                    moved = error.InnerException is InvalidOperationException;
                }

                Check(moved && calls.Restored.Count == 0, "a projection from a different native start is rejected", ref passed);
            }

            var font = new Font(calls);
            foreach (int original in new[]
            {
                0,
                -1,
                9999999
            }

            )
                foreach (bool italic in new[]
                {
                    false,
                    true
                }

                )
                {
                    calls.Reset();
                    calls.Italic = original;
                    bool changed = WordSelection.ApplyItalic(scope, font, new ItalicSpan(0, 6, true, italic));
                    int wanted = italic ? -1 : 0;
                    Check(changed == (original != wanted) && calls.Italic == wanted && calls.StyleWrites == (original == wanted ? 0 : 1), "already-correct styles avoid a native write; mixed and changed styles are applied", ref passed);
                }

            calls.Reset();
            calls.Bold = -1;
            Check(!WordSelection.ApplyItalic(scope, font, new ItalicSpan(0, 6)) && calls.StyleWrites == 0 && calls.Italics == 0, "ordinary bold protection remains before style reads or writes", ref passed);
            Console.WriteLine("Word transaction reuse: " + passed + " assertions; no repeated revision scan or unchanged style writes, exact native ownership and one undo record.");
            return passed;
        }
    }
}
