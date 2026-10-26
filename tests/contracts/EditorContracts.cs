// One scenario matrix shared by editor-capability profiles. These are deterministic
// selection models around production guards/policy, not claims of live app testing.
using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    public static class EditorContracts
    {
        public sealed class Matrix
        {
            public int schema;
            public Profile[] profiles;
            public Example[] cases;
        }

        public sealed class Profile
        {
            public string id;
            public int range_delay;
            public bool ignore_range, keyboard_first;
        }

        public sealed class Example
        {
            public string input, expected;
        }

        // All profiles use the same surface. Only observed selection capabilities
        // vary, so a new regression cannot silently be omitted for one profile.
        sealed class Surface : ISelectionSurface
        {
            readonly Profile profile;
            readonly int ownedStart, ownedEnd;
            readonly string expected;
            int start, end, ticks, pending = -1;
            public string Text;
            public bool Context = true, IgnoreAll;
            public Action Selecting;
            public int Waits
            {
                get
                {
                    return ticks;
                }
            }

            public Surface(Profile value, string prefix, string target, string suffix)
            {
                profile = value;
                Text = prefix + target + suffix;
                expected = target;
                ownedStart = prefix.Length;
                ownedEnd = ownedStart + target.Length;
                start = end = ownedEnd;
            }

            public bool AtOwnedCaret()
            {
                int located;
                return start == end && start == ownedEnd && EditGuard.Locate(Text, start, end, expected, out located) && located == ownedStart;
            }

            public bool ExactSelection()
            {
                return start == ownedStart && end == ownedEnd && end <= Text.Length && Text.Substring(start, end - start) == expected;
            }

            public void RequestSelection()
            {
                if (Selecting != null)
                    Selecting();
                if (!profile.ignore_range && !IgnoreAll)
                {
                    if (profile.range_delay == 0)
                        Select();
                    else
                        pending = ticks + profile.range_delay;
                }
            }

            void Select()
            {
                start = ownedStart;
                end = ownedEnd;
                pending = -1;
            }

            public bool RequestKeyboardSelection()
            {
                if (Selecting != null)
                    Selecting();
                if (!IgnoreAll)
                    pending = ticks + 1;
                return true;
            }

            public bool RestoreCaret()
            {
                if (!Context || !ExactSelection() && !AtOwnedCaret())
                    return false;
                start = end = ownedEnd;
                pending = -1;
                return true;
            }

            public void Wait()
            {
                ticks++;
                if (pending >= 0 && ticks >= pending)
                    Select();
            }

            public bool Replace(string output)
            {
                if (!SelectionCommit.CanPaste(this, () => Context))
                    return false;
                Text = Text.Substring(0, start) + output + Text.Substring(end);
                end = start + output.Length;
                return true;
            }

            public void LoseSelection()
            {
                start = end;
            }
        }

        static void Require(bool value, string message, ref int assertions)
        {
            if (!value)
                throw new InvalidOperationException("Editor contract: " + message);
            assertions++;
        }

        static void CheckProfile(Core core, Profile profile, Example example, string mode, string prefix, ref int assertions)
        {
            string suffix = " | Earlier text stays here";
            string expected = mode == "balaram" ? EncodingMap.Balaram(example.expected) : example.expected;
            string output = core.ConvertHighlighted(example.input, mode);
            string tag = profile.id + "/" + mode + "/" + example.input;
            Require(output == expected, tag + " spelling/English/number policy; expected=" + expected + "; actual=" + output, ref assertions);
            var surface = new Surface(profile, prefix, example.input, suffix);
            Require(SelectionCommit.Prepare(surface, () => surface.Context, surface.Wait, profile.keyboard_first), tag + " prepares", ref assertions);
            Require(surface.Replace(output), tag + " commits", ref assertions);
            Require(surface.Text == prefix + expected + suffix, tag + " exact target only", ref assertions);
            Require(surface.Waits <= 13, tag + " finite selection wait", ref assertions);
            // Cancel after selection was requested, including synchronous providers.
            surface = new Surface(profile, prefix, example.input, suffix);
            Surface lost = surface;
            surface.Selecting = () => lost.Context = false;
            Require(!SelectionCommit.Prepare(surface, () => surface.Context, surface.Wait, profile.keyboard_first), tag + " focus change cancels", ref assertions);
            Require(!surface.Replace(output) && surface.Text == prefix + example.input + suffix, tag + " no stale write", ref assertions);
            surface = new Surface(profile, prefix, example.input, suffix);
            Require(SelectionCommit.Prepare(surface, () => surface.Context, surface.Wait, profile.keyboard_first), tag + " prepares collapse test", ref assertions);
            surface.LoseSelection();
            Require(!surface.Replace(output) && surface.Text == prefix + example.input + suffix, tag + " collapsed selection cannot append", ref assertions);
            surface = new Surface(profile, prefix, example.input, suffix)
            {
                IgnoreAll = true
            };
            Require(!SelectionCommit.Prepare(surface, () => surface.Context, surface.Wait, profile.keyboard_first), tag + " ignored provider skips", ref assertions);
            Require(surface.Waits <= 13 && surface.Text == prefix + example.input + suffix, tag + " ignored provider bounded", ref assertions);
        }

        static void CheckCycles(Core core, Profile profile, string mode, ref int assertions)
        {
            IntPtr owner = new IntPtr(42);
            int[] field =
            {
                7,
                19
            };
            string output = core.Word("raja", mode);
            var cycle = new CaretWordCycle();
            cycle.Start(output, core.CaretChoices("raja", mode), mode, owner, field, "raja");
            for (int n = 0; n < 12; n++)
            {
                string next;
                Require(cycle.Next(output, mode, owner, field, out next), profile.id + " repeated cycle", ref assertions);
                cycle.Commit(next);
                output = next;
                Require(cycle.Original == "raja", profile.id + " original survives all cycles for undo", ref assertions);
            }

            string rejected;
            Require(!cycle.Next(output, mode, owner, new[] { 7, 20 }, out rejected), profile.id + " another field cannot use cycle", ref assertions);
        }

        public static int Run(string root, string matrixPath)
        {
            var matrix = new JavaScriptSerializer().Deserialize<Matrix>(File.ReadAllText(matrixPath));
            string[] required =
            {
                "word",
                "browser-plain",
                "browser-rich",
                "indesign-text",
                "indesign-find",
                "excel-cell"
            };
            if (matrix == null || matrix.schema != 1 || matrix.profiles == null || matrix.cases == null || matrix.cases.Length == 0 || !matrix.profiles.Select(profile => profile.id).OrderBy(id => id).SequenceEqual(required.OrderBy(id => id)) || matrix.profiles.Any(profile => profile.range_delay < 0 || profile.range_delay > 3) || matrix.cases.Any(example => string.IsNullOrEmpty(example.input) || string.IsNullOrEmpty(example.expected)))
                throw new InvalidDataException("Incomplete editor contract matrix");
            int assertions = 0;
            using (var core = new Core(root))
            {
                foreach (var profile in matrix.profiles)
                {
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        foreach (var example in matrix.cases)
                            foreach (string prefix in new[]
                            {
                                "",
                                "Earlier an age since ",
                                "Earlier paragraph\n"
                            }

                            )
                                CheckProfile(core, profile, example, mode, prefix, ref assertions);
                        CheckCycles(core, profile, mode, ref assertions);
                    }
                }

                // Exercise host-specific pure guards as well as the shared surface.
                string grep = "\\bprasada\\b [a-z] $1";
                Require(InDesignFindSyntax.ConvertRange(grep, 0, grep.Length, value => core.ConvertHighlighted(value, "unicode")) == "\\bprasāda\\b [a-z] $1", "InDesign GREP syntax retained", ref assertions);
                Require(!ExcelCellField.TextBuffer("=raja") && !ExcelCellField.TextBuffer("+raja") && ExcelCellField.TextBuffer("raja"), "Excel formula and edit-buffer guards", ref assertions);
                var visible = WordSelection.VisibleMap.Create("rāaja", 0, 5, new[] { 2 }, new[] { 3 });
                Require(visible != null && visible.Value == "rāja" && visible.NativeOffset(2) == 3, "Word tracked text maps visible target", ref assertions);
                Require(SelectionConversion.PastedTextMatches("rāja\nKṛṣṇa", "rāja\r\nKṛṣṇa") && !SelectionConversion.PastedTextMatches("rāja\nKṛṣṇa", "rāja Kṛṣṇa"), "Browser paragraph normalization remains exact", ref assertions);
                var spans = core.ItalicWords("Earlier rāja and Kṛṣṇa", "unicode");
                Require(spans.Any(span => span.Start == 8 && span.Length == 4) && spans.All(span => span.Start == 8), "Italic policy protects English and proper names", ref assertions);
            }

            Console.WriteLine("Shared editor contracts: " + matrix.profiles.Length + " capability profiles; " + matrix.cases.Length + " scenarios; " + assertions + " assertions; no native UI.");
            return assertions;
        }
    }
}
