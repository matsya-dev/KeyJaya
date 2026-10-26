// Noninteractive regressions: bounded cell reads, formula exclusion, caret positions, and edit-mode targeting.
// Run: .\test.ps1 -Suite ExcelCellTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KeyJaya
{
    public static class ExcelCellTests
    {
        static int passed;
        static void Check(bool value, string label)
        {
            if (!value)
                throw new Exception(label);
            passed++;
        }

        static void BlockedProvider()
        {
            var reader = new BoundedCellRead<string>();
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                int calls = 0;
                string result;
                var clock = Stopwatch.StartNew();
                Check(!reader.TryRead(() =>
                {
                    Interlocked.Increment(ref calls);
                    entered.Set();
                    release.Wait();
                    return "STALE";
                }, 40, out result) && result == null, "Blocked provider returns no authorization");
                Check(clock.ElapsedMilliseconds < 2000, "Blocked provider has a finite foreground wait");
                Check(entered.Wait(2000), "Provider request started");
                for (int n = 0; n < 100; n++)
                    Check(!reader.TryRead(() =>
                    {
                        Interlocked.Increment(ref calls);
                        return "WRONG";
                    }, 40, out result) && result == null, "Pending provider does not spawn another request");
                Check(calls == 1, "Only one stalled request can remain outstanding");
                release.Set();
                string fresh = null;
                Check(SpinWait.SpinUntil(() => reader.TryRead(() => "FRESH", 100, out fresh), 3000), "Provider can recover after stalled request completes");
                Check(fresh == "FRESH", "Late stale result is never reused for a new conversion");
            }
        }

        static void ProviderFailures()
        {
            var reader = new BoundedCellRead<string>();
            string result;
            Check(!reader.TryRead(null, 100, out result), "Null reader is refused");
            foreach (int invalid in new[]
            {
                -1,
                0,
                1001,
                int.MaxValue
            }

            )
                Check(!reader.TryRead(() => "WRONG", invalid, out result), "Invalid timeout creates no request");
            Check(!reader.TryRead(() =>
            {
                throw new InvalidOperationException("Unavailable provider");
            }, 1000, out result) && result == null, "Provider exception safely returns no result");
            Check(!reader.TryRead(() => null, 1000, out result), "Missing Formula Bar returns no result");
            Check(reader.TryRead(() => "rāja", 1000, out result) && result == "rāja", "Failed read does not poison later discovery");
            int active = 0, peak = 0;
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var first = Task.Factory.StartNew(() =>
                {
                    string value;
                    return reader.TryRead(() =>
                    {
                        int count = Interlocked.Increment(ref active);
                        Interlocked.Exchange(ref peak, count);
                        entered.Set();
                        release.Wait();
                        Interlocked.Decrement(ref active);
                        return "original";
                    }, 1000, out value);
                });
                Check(entered.Wait(3000), "Concurrent test request entered provider");
                try
                {
                    for (int n = 0; n < 30; n++)
                        Check(!reader.TryRead(() =>
                        {
                            Interlocked.Increment(ref active);
                            return "WRONG";
                        }, 10, out result), "Concurrent caller skips busy provider");
                }
                finally
                {
                    release.Set();
                }

                Check(first.Wait(3000) && first.Result && peak == 1 && active == 0, "Concurrent access preserves one-provider-request limit");
            }
        }

        static void Recovery()
        {
            foreach (bool target in new[]
            {
                false,
                true
            }

            )
                foreach (bool output in new[]
                {
                    false,
                    true
                }

                )
                    Check(SelectionConversion.UntouchedManualInput(target, output) == (!target && !output), "Only untouched discovery can bypass unavailable text provider");
            var owner = new IntPtr(51);
            var hold = new Native.CorrectionInputHold();
            Check(hold.Begin(owner), "Typing queue starts before discovery");
            var expected = new uint[]
            {
                0x42,
                0x42,
                0xA0,
                0x45,
                0x45,
                0xA0
            };
            for (int n = 0; n < expected.Length; n++)
            {
                bool up = n == 1 || n == 4 || n == 5;
                Check(hold.Keyboard(new Native.KeyData { vk = expected[n], flags = up ? 0x80u : 0u }, up ? 0x101 : 0x100, owner), "Following input is queued during lookup");
            }

            Check(SelectionConversion.UntouchedManualInput(false, false), "Failed discovery permits replay at original user selection");
            for (int n = 0; n < expected.Length; n++)
            {
                var events = hold.DrainNext(owner);
                Check(events.Length == 1 && events[0].union.keyboard.vk == expected[n], "Letters and Shift replay in physical order");
                Check(((events[0].union.keyboard.flags & 2) != 0) == (n == 1 || n == 4 || n == 5), "Modifier releases are not lost during recovery");
            }

            Check(hold.DrainNext(owner).Length == 0 && !hold.Active, "Failed lookup cannot retain the keyboard queue");
            Check(!hold.Keyboard(new Native.KeyData { vk = 0x46 }, 0x100, owner), "Typing resumes normally after failed discovery");
        }

        public static int Main()
        {
            BlockedProvider();
            ProviderFailures();
            Recovery();
            foreach (string name in new[]
            {
                "EXCEL6",
                "EXCEL<"
            }

            )
                Check(ExcelCellField.EditorClass(name), "Cell and Formula Bar editors share bounded reads and formula protection");
            foreach (string name in new[]
            {
                null,
                "",
                "EXCEL7",
                "Excel6",
                "EXCEL< extra",
                "Edit",
                "NetUIHWND",
                "Search"
            }

            )
                Check(!ExcelCellField.EditorClass(name), "Unrelated controls cannot authorize Excel edit-buffer access");
            foreach (string prefix in new[]
            {
                "",
                "Earlier untouched ",
                "(prefix) "
            }

            )
                foreach (string word in new[]
                {
                    "Kṛṣṇa",
                    "räja",
                    "Śrīla Prabhupāda",
                    "कृष्ण"
                }

                )
                    foreach (string tail in new[]
                    {
                        "",
                        ".",
                        ",",
                        "?!",
                        "),",
                        "’"
                    }

                    )
                    {
                        string full = prefix + word + tail + " next";
                        int start = prefix.Length, end = start + word.Length;
                        for (int offset = 0; offset <= word.Length + tail.Length; offset++)
                        {
                            int at = ExcelCellField.CaretPosition(full, start, end, word, tail, offset);
                            Check(at == start + offset && full.Substring(0, start) == prefix && at <= end + tail.Length, "Caret stays within converted output or its verified punctuation");
                        }

                        Check(ExcelCellField.CaretPosition(full, start, end, word, tail, word.Length + tail.Length + 1) == -1, "Caret cannot enter the following word");
                        Check(ExcelCellField.CaretPosition(full, start, end, word, tail, -1) == -1, "Negative offset is refused");
                        Check(ExcelCellField.CaretPosition(full, start, end, word, tail + " next", word.Length + 1) == -1, "A following English word cannot be presented as punctuation");
                        Check(ExcelCellField.CaretPosition(full, start, end, word, "!" + tail, word.Length) == -1, "Changed punctuation cannot authorize navigation");
                    }

            Check(ExcelCellField.CaretPosition("raja.", 0, 4, "rāja", ".", 5) == -1, "Changed output cannot authorize navigation");
            Check(ExcelCellField.CaretPosition(null, 0, 0, "", "", 0) == -1 && ExcelCellField.CaretPosition("raja", -1, 4, "raja", "", 0) == -1 && ExcelCellField.CaretPosition("raja", 0, 5, "raja", "", 0) == -1, "Invalid buffers and bounds are refused");
            foreach (string mode in new[]
            {
                "Enter",
                "Edit",
                "Ready"
            }

            )
            {
                Check(ExcelCellField.ModeLabel("Cell Mode", mode) == mode, "Status value identifies the exact cell mode");
                Check(ExcelCellField.ModeLabel("Cell Mode " + mode, null) == mode, "Status label identifies the exact cell mode");
            }

            foreach (string name in new[]
            {
                null,
                "",
                "Enter",
                "Edit",
                "Not Cell Mode",
                "Cell Mode Enter extra"
            }

            )
                Check(ExcelCellField.ModeLabel(name, "Enter") == null, "Unrelated or ambiguous labels cannot authorize F2");
            foreach (string value in new[]
            {
                null,
                "",
                "enter",
                "Point",
                "Calculating",
                "Edit extra"
            }

            )
                Check(ExcelCellField.ModeLabel("Cell Mode", value) == null, "Unknown modes cannot authorize navigation");
            foreach (string valid in new[]
            {
                "",
                "Earlier untouched raja",
                "ā śāstra",
                "räja",
                "कृष्ण",
                "কৃষ্ণ",
                "123",
                new string ('x', 4096)
            }

            )
                Check(ExcelCellField.TextBuffer(valid), "Owned plain-text cell buffer accepted");
            foreach (string invalid in new[]
            {
                null,
                "=SUM(A1:A2)",
                "+A1",
                "-A1",
                "-SUM(A1)",
                "@SUM(A1)",
                "bad\0text",
                "two\nlines",
                "two\rlines",
                "tab\tvalue",
                new string ('x', 4097)
            }

            )
                Check(!ExcelCellField.TextBuffer(invalid), "Formula or ambiguous cell buffer remains untouched");
            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
