// Read-only fake Word object model: the production reader has no writable members.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace KeyJaya
{
    public static class WordShortcutContracts
    {
        public sealed class Binding
        {
            public string Command
            {
                get
                {
                    if (Broken)
                        throw new InvalidOperationException();
                    return CommandValue;
                }
            }

            public int KeyCategory
            {
                get
                {
                    return CategoryValue;
                }
            }

            public string CommandValue;
            public int CategoryValue;
            public bool Broken;
        }

        public sealed class ApplicationModel
        {
            public readonly Dictionary<int, Binding> Bindings = new Dictionary<int, Binding>();
            public int Reads;
            public int BuildKeyCode(object key, object second = null, object third = null, object fourth = null)
            {
                return new[]
                {
                    key,
                    second,
                    third,
                    fourth
                }.Where(x => x != null && x != Type.Missing).Aggregate(0, (value, item) => value | Convert.ToInt32(item));
            }

            [IndexerName("FindKey")]
            public Binding this[object code, object second = null ]
            {
                get
                {
                    Reads++;
                    return Bindings[Convert.ToInt32(code)];
                }
            }
        }

        static int passed;
        static void Check(bool ok, string label)
        {
            if (!ok)
                throw new Exception(label);
            passed++;
        }

        public static int Run()
        {
            passed = 0;
            var defaults = WordShortcutInspector.Keys(new ShortcutSettings(null));
            Check(defaults.Length == 13 && defaults.All(x => x.Shortcut != "Left Ctrl"), "only actual Word chords are queried; standalone modifier taps excluded");
            var navigation = defaults.Single(x => x.Shortcut == "Ctrl+Shift+Left");
            Check(WordShortcutInspector.KeyArguments(navigation).Take(3).SequenceEqual(new object[] { 37, 512, 256 }), "Word Control/Shift constants are correct");
            for (uint modifiers = 0; modifiers < 8; modifiers++)
            {
                var args = WordShortcutInspector.KeyArguments(new WordShortcutKey { Key = 0x4B, Modifiers = modifiers });
                int code = args.Where(x => x != Type.Missing).Aggregate(0, (value, item) => value | Convert.ToInt32(item));
                Check(code == (0x4B | ((modifiers & 2) != 0 ? 512 : 0) | ((modifiers & 4) != 0 ? 256 : 0) | ((modifiers & 1) != 0 ? 1024 : 0)), "all modifier combinations map to Word, not RegisterHotKey values");
            }

            Check(!WordShortcutInspector.Classify(navigation, "WordLeftExtend", 1).Review, "normal navigation is not flagged");
            Check(WordShortcutInspector.Classify(navigation, "RejectChangesAndAdvance", 1).Review, "reported original Word conflict is detected");
            Check(WordShortcutInspector.Classify(navigation, "WordLeftExtend", 2).Review, "a macro with the expected command's name is not mistaken for the built-in");
            Check(WordShortcutInspector.Classify(navigation, "", 0).Review && WordShortcutInspector.Classify(navigation, "", -1).Review, "disabled and missing navigation are reported");
            var conversion = defaults.Single(x => x.Shortcut == "Ctrl+.");
            Check(WordShortcutInspector.Classify(conversion, "Superscript", 1).Review, "KeyJaya and Word overlap is reported");
            Check(!WordShortcutInspector.Classify(conversion, "", 0).Review && !WordShortcutInspector.Classify(conversion, "", -1).Review, "disabled and unassigned Word commands are not false overlaps");
            Check(WordShortcutInspector.Classify(conversion, "", 7).Command == "Word shortcut prefix" && WordShortcutInspector.Classify(conversion, "", 7).Review, "unnamed Word prefix assignments are explained");
            var app = new ApplicationModel();
            app.Bindings[37 | 512 | 256] = new Binding
            {
                CommandValue = "RejectChangesAndAdvance",
                CategoryValue = 1
            };
            int released = 0;
            var rows = WordShortcutInspector.ReadBindings(app, new[] { navigation }, () => true, value => released++);
            Check(rows.Count == 1 && rows[0].Review && rows[0].Command == "RejectChangesAndAdvance" && released == 1 && app.Reads == 1, "actual getter-only reader reports conflict and releases binding once");
            app.Reads = 0;
            try
            {
                WordShortcutInspector.ReadBindings(app, new[] { navigation }, () => false, value => released++);
                throw new Exception("Stale owner accepted");
            }
            catch (InvalidOperationException)
            {
                Check(app.Reads == 0, "stale ownership stops before querying a binding");
            }

            int checks = 0;
            try
            {
                WordShortcutInspector.ReadBindings(app, new[] { navigation, navigation }, () => ++checks == 1, value => released++);
                throw new Exception("Changed owner accepted");
            }
            catch (InvalidOperationException)
            {
                Check(app.Reads == 1 && released == 2, "ownership change after read rejects the report and releases the binding");
            }

            app.Bindings[37 | 512 | 256].Broken = true;
            try
            {
                WordShortcutInspector.ReadBindings(app, new[] { navigation }, () => true, value => released++);
                throw new Exception("Failed read accepted");
            }
            catch (System.Reflection.TargetInvocationException)
            {
                Check(released == 3, "failed property read releases the binding");
            }

            var custom = WordShortcutInspector.Keys(new ShortcutSettings(null, new Dictionary<string, string> { { "word", "F8" }, { "letter", "Right Ctrl" } }));
            Check(custom.Any(x => x.Shortcut == "F8") && !custom.Any(x => x.Shortcut == "`"), "check uses the user's current proposed bindings");
            Check(!TrayMenu.Items(false, true, true).Any(x => x.Id == 36 || x.Id == 37), "removed spelling editor and Word overlap actions stay out of the tray");
            var isolated = WordShortcutInspector.Check(new ShortcutSettings(null));
            Check(isolated.Complete && isolated.Status == "Isolated test helper", "production parent starts only its hidden owned helper and decodes its report");
            var invalid = WordShortcutInspector.Check(new ShortcutSettings(null, new Dictionary<string, string> { { "letter", "F23" } }));
            Check(!invalid.Complete && invalid.Status.Contains("could not finish"), "malformed helper output is an unavailable report, never a clean result");
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var stalled = WordShortcutInspector.Check(new ShortcutSettings(null, new Dictionary<string, string> { { "word", "F24" } }));
            Check(!stalled.Complete && stalled.Status.Contains("five seconds") && watch.ElapsedMilliseconds < 8000, "a stalled owned helper is terminated within the deadline without touching Word");
            Console.WriteLine("Word shortcut inspection: " + passed + " assertions; getter-only access, navigation conflicts, overlaps and ownership.");
            return passed;
        }
    }
}
