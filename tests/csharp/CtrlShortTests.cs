// Noninteractive regressions: abbreviation priority, one-time upgrades, Ctrl scripture, and paired ranges.
// Run: .\test.ps1 -Suite CtrlShortTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace KeyJaya
{
    public static class CtrlShortTests
    {
        static int passed, failed;
        static void Equal(string expected, string actual, string name)
        {
            if (expected == actual)
                passed++;
            else
            {
                failed++;
                Console.WriteLine("FAIL " + name + " expected=" + expected + " actual=" + actual);
            }
        }

        static void Check(bool value, string name)
        {
            Equal("True", value.ToString(), name);
        }

        static void SsrAddition(string source)
        {
            string title = "The Science of Self-Realization", root = Path.Combine(Path.GetDirectoryName(typeof(CtrlShortTests).Assembly.Location), "ssr-addition-artifacts", Guid.NewGuid().ToString("N"));
            foreach (string scenario in new[]
            {
                "new",
                "custom",
                "disabled",
                "empty"
            }

            )
            {
                string app = Path.Combine(root, scenario);
                Directory.CreateDirectory(Configuration.Folder(app));
                Directory.CreateDirectory(Path.Combine(app, "assets"));
                Directory.CreateDirectory(Path.Combine(app, "state"));
                File.Copy(Configuration.Factory(source), Configuration.Factory(app));
                File.WriteAllText(Path.Combine(app, "state", "sbb-trigger-added.flag"), "Already applied");
                File.WriteAllText(Path.Combine(app, "state", "bb-trigger-added.flag"), "Already applied");
                string original = scenario == "empty" ? "matches: []\r\n" : scenario == "custom" ? "# My custom SSR stays.\r\nmatches:\r\n  - trigger: 'SSR'\r\n    replace: 'My own replacement'\r\n" : scenario == "disabled" ? "matches:\r\n  - trigger: 'SSR>'\r\n    replace: 'Disabled replacement'\r\n    enabled: false\r\n" : "# Preserve my comments.\r\nmatches:\r\n  - trigger: 'MINE'\r\n    replace: 'Personal text'\r\n";
                string path = Configuration.Short(app);
                File.WriteAllText(path, original, new UTF8Encoding(true));
                byte[] before = File.ReadAllBytes(path);
                bool changed = Configuration.AddRequestedShortTriggers(app);
                var exact = new HashSet<string>(StringComparer.Ordinal);
                var rows = Configuration.Read(path, false, true, exact);
                Check(!rows.Keys.Any(key => key.StartsWith("SBB", StringComparison.Ordinal)), "SSR upgrade does not revive deleted SBB " + scenario);
                if (scenario == "custom" || scenario == "disabled")
                {
                    Check(!changed && File.ReadAllBytes(path).SequenceEqual(before), "existing SSR group is byte-identical " + scenario);
                    Check(rows.Count == 1, "no missing SSR forms added over user configuration " + scenario);
                    if (scenario == "disabled")
                        Check(Configuration.Read(path).Count == 0, "disabled SSR remains disabled");
                }
                else
                {
                    Check(changed, "SSR added to existing or empty configuration " + scenario);
                    foreach (string key in new[]
                    {
                        "SSR",
                        "SSR>",
                        "SSR<",
                        "SSR?"
                    }

                    )
                    {
                        Equal(title, rows[key], "upgrade title " + scenario + key);
                        Check(exact.Contains(key), "upgrade retains exact-title metadata " + scenario + key);
                    }

                    if (scenario == "new")
                        Check(File.ReadAllText(path).StartsWith(original, StringComparison.Ordinal), "unrelated existing trigger and comments unchanged");
                    Check(File.ReadAllBytes(path).Take(3).SequenceEqual(before.Take(3)), "SSR addition retains UTF-8 BOM " + scenario);
                }

                Check(File.Exists(Path.Combine(app, "state", "ssr-trigger-added.flag")), "independent SSR upgrade marker " + scenario);
                byte[] applied = File.ReadAllBytes(path);
                Check(!Configuration.AddRequestedShortTriggers(app) && File.ReadAllBytes(path).SequenceEqual(applied), "second SSR upgrade is a no-op " + scenario);
                Configuration.Write(path, new Dictionary<string, string> { { "MINE", "Personal text" } });
                Check(!Configuration.AddRequestedShortTriggers(app) && Configuration.Read(path).Count == 1, "deleted SSR remains deleted on later upgrade " + scenario);
            }

            var factory = Configuration.Read(Path.Combine(source, "assets", "short-trigger-defaults.yml"));
            foreach (string key in new[]
            {
                "SSR",
                "SSR>",
                "SSR<",
                "SSR?"
            }

            )
                Equal(title, factory[key], "factory SSR title " + key);
            Check(File.ReadAllBytes(Configuration.Short(source)).SequenceEqual(File.ReadAllBytes(Configuration.Factory(source))), "fresh trigger YAML is an exact copy of the authoritative factory list");
        }

        static void BbAddition(string source)
        {
            string root = Path.Combine(Path.GetDirectoryName(typeof(CtrlShortTests).Assembly.Location), "bb-addition-artifacts", Guid.NewGuid().ToString("N"));
            foreach (string scenario in new[]
            {
                "missing",
                "custom",
                "disabled",
                "partial",
                "empty"
            }

            )
            {
                string app = Path.Combine(root, scenario);
                Directory.CreateDirectory(Configuration.Folder(app));
                Directory.CreateDirectory(Path.Combine(app, "assets"));
                Directory.CreateDirectory(Path.Combine(app, "state"));
                File.Copy(Configuration.Factory(source), Configuration.Factory(app));
                foreach (string name in new[]
                {
                    "sbb",
                    "ssr"
                }

                )
                    File.WriteAllText(Path.Combine(app, "state", name + "-trigger-added.flag"), "Previously applied");
                string original = scenario == "empty" ? "matches: []\r\n" : scenario == "custom" ? "matches:\r\n  - trigger: 'BB'\r\n    replace: 'My custom BB'\r\n" : scenario == "disabled" ? "matches:\r\n  - trigger: 'BB>'\r\n    replace: 'Disabled BB'\r\n    enabled: false\r\n" : scenario == "partial" ? "matches:\r\n  - trigger: 'BB?'\r\n    replace: 'Custom plain BB'\r\n" : "# My existing triggers remain.\r\nmatches:\r\n  - trigger: 'MINE'\r\n    replace: 'Personal text'\r\n";
                string path = Configuration.Short(app);
                File.WriteAllText(path, original, new UTF8Encoding(true));
                byte[] before = File.ReadAllBytes(path);
                bool changed = Configuration.AddRequestedShortTriggers(app);
                var rows = Configuration.Read(path, false, true);
                if (scenario == "custom" || scenario == "disabled" || scenario == "partial")
                    Check(!changed && File.ReadAllBytes(path).SequenceEqual(before), "existing BB group stays byte-identical " + scenario);
                else
                {
                    Check(!changed && File.ReadAllBytes(path).SequenceEqual(before), "first upgrade preserves absent BB without provenance " + scenario);
                    Check(!rows.Keys.Any(key => TriggerEditorStore.Group(key) == "BB"), "pre-upgrade BB deletion is not restored " + scenario);
                }

                Check(!File.Exists(Path.Combine(app, "state", "bb-trigger-added.flag")), "ambiguous BB absence creates no repair marker " + scenario);
                byte[] applied = File.ReadAllBytes(path);
                Check(!Configuration.AddRequestedShortTriggers(app) && File.ReadAllBytes(path).SequenceEqual(applied), "BB upgrade is idempotent");
                Configuration.Write(path, new Dictionary<string, string> { { "MINE", "Personal text" } });
                Check(!Configuration.AddRequestedShortTriggers(app) && Configuration.Read(path).Count == 1, "BB deletion without a marker stays respected on repeated upgrades");
            }

            var factory = Configuration.Read(Configuration.Factory(source));
            Equal("Bṛhad-bhāgavatāmṛta", factory["BB"], "fresh installs still include BB");
            Equal(factory["BB"], factory["BB>"], "fresh BB Unicode form");
            Equal("Båhad-bhägavatämåta", factory["BB<"], "fresh BB legacy form");
            Equal("Brhad-bhagavatamrta", factory["BB?"], "fresh BB plain form");
            // A user can still add BB and explicitly restore its factory forms.
            // Saving that edit is distinct from an upgrade inferring user intent.
            string restoreRoot = Path.Combine(root, "empty");
            var editor = new TriggerEditorStore(Configuration.Short(restoreRoot), Configuration.Factory(restoreRoot));
            var entry = editor.NewEntry();
            entry.Name = "BB";
            entry.Replacement = "Temporary editor value";
            editor.Upsert(null, entry);
            editor.Restore(entry);
            editor.Save(null);
            var restored = Configuration.Read(Configuration.Short(restoreRoot));
            foreach (string key in new[]
            {
                "BB",
                "BB>",
                "BB<",
                "BB?"
            }

            )
                Equal(factory[key], restored[key], "explicit user restoration remains available " + key);
            Check(restored["MINE"] == "Personal text", "explicit BB restoration preserves unrelated trigger edits");
        }

        static void AcbspUpgrade()
        {
            string root = Path.Combine(Path.GetDirectoryName(typeof(CtrlShortTests).Assembly.Location), "acbsp-upgrade-artifacts", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Configuration.Folder(root));
            string original = "# Preserve my header.\r\nmatches:\r\n  - trigger: 'ACBSP>'\r\n    replace: 'A. C. Bhaktivedanta Swami Prabhupāda'\r\n    # Preserve this comment.\r\n  - trigger: 'ACBSP<'\r\n    replace: A. C. Bhaktivedanta Swami Prabhupäda # Disabled variant\r\n    enabled: false\r\n  - trigger: 'ACBSP?'\r\n    replace: 'My custom plain output'\r\n  - trigger: 'ACBSP'\r\n    replace: 'A. C. Bhaktivedanta Swami Prabhupāda'\r\n    keep_exact: true\r\n  - trigger: 'MINE'\r\n    replace: |-\r\n      First line\r\n      Second line\r\n";
            string path = Configuration.Short(root);
            File.WriteAllText(path, original, new UTF8Encoding(true));
            byte[] before = File.ReadAllBytes(path);
            Check(Configuration.AddRequestedShortTriggers(root), "upgrade applies requested ACBSP correction even without SBB additions");
            var all = Configuration.Read(path, false, true);
            Equal("A.C. Bhaktivedanta Swami Prabhupāda", all["ACBSP>"], "upgrade changes only old factory Unicode wording");
            Equal("A.C. Bhaktivedanta Swami Prabhupäda", all["ACBSP<"], "upgrade changes disabled old factory wording");
            Check(!Configuration.Read(path).ContainsKey("ACBSP<"), "disabled ACBSP stays disabled");
            Equal("My custom plain output", all["ACBSP?"], "custom ACBSP override preserved");
            Equal("A. C. Bhaktivedanta Swami Prabhupāda", all["ACBSP"], "explicit keep-exact replacement preserved");
            string expected = original.Replace("replace: 'A. C. Bhaktivedanta Swami Prabhupāda'\r\n    #", "replace: 'A.C. Bhaktivedanta Swami Prabhupāda'\r\n    #").Replace("replace: A. C. Bhaktivedanta Swami Prabhupäda #", "replace: A.C. Bhaktivedanta Swami Prabhupäda #");
            Equal(expected, File.ReadAllText(path), "comments, newlines and unrelated YAML unchanged");
            Check(File.ReadAllBytes(path).Take(3).SequenceEqual(before.Take(3)), "UTF-8 BOM retained");
            Check(File.ReadAllBytes(Path.Combine(root, "backups", "acbsp-initials-previous.yml")).SequenceEqual(before), "original trigger file backed up exactly");
            byte[] updated = File.ReadAllBytes(path);
            Check(!Configuration.AddRequestedShortTriggers(root) && File.ReadAllBytes(path).SequenceEqual(updated), "second upgrade is a byte-identical no-op");
            Configuration.Write(path, new Dictionary<string, string> { { "MINE", "Only my trigger" } });
            Check(!Configuration.UpdateAcbspInitials(root) && Configuration.Read(path).Count == 1, "deleted ACBSP variants never restored");
            File.WriteAllText(path, "matches:\n  - trigger: 'ACBSP:'\n    replace: A. C. Bhaktivedanta Swami Prabhupada # Old form was A. C. Bhaktivedanta Swami Prabhupada\n");
            Check(Configuration.UpdateAcbspInitials(root), "legacy plain suffix upgraded");
            Equal("A.C. Bhaktivedanta Swami Prabhupada", Configuration.Read(path)["ACBSP?"], "legacy plain output updated");
            Check(File.ReadAllText(path).Contains("# Old form was A. C. Bhaktivedanta Swami Prabhupada"), "matching words inside comments never edited");
        }

        static void ScriptureCtrl(Core c)
        {
            foreach (string reference in new[]
            {
                "BG2.1",
                "BG2.1-3",
                "BG2.1-3VT",
                "BG18.66",
                "BG18.66VT"
            }

            )
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string expanded = c.CtrlExpansion(reference, mode);
                    Check(!string.IsNullOrEmpty(expanded) && Native.TextNeedsClipboard(expanded, false), "Real multiline verse output uses clipboard even in a plain-text editor " + reference + " " + mode);
                    Check(Native.TextNeedsClipboard(expanded, true), "Formatted verses use clipboard " + reference + " " + mode);
                }

            foreach (string text in new[]
            {
                "rāja",
                "Kṛṣṇa",
                "Śrīla Prabhupāda",
                new string ('x', 160)
            }

            )
            {
                Check(!Native.TextNeedsClipboard(text, false), "Short ordinary conversion retains the direct-input path");
                Check(Native.TextNeedsClipboard(text, true), "Rich formatting requires the clipboard even for a short word");
            }

            foreach (string text in new[]
            {
                "verse\nline",
                "verse\r\nline",
                new string ('x', 161),
                "ā\tī",
                "😀"
            }

            )
                Check(Native.TextNeedsClipboard(text, false), "Unsupported direct text routes to paste without truncation");
            string[] references =
            {
                "BG18.66",
                "BG18.66V",
                "BG18.66T",
                "BG18.66VT",
                "BG18.66VTR",
                "SB1.1.1",
                "CC1.1.1",
                "CCAdi1.1",
                "CB2.3.4",
                "BS5.1",
                "NOI1",
                "ISOINV",
                "BG2.13-14",
                "BG2.13–14",
                "BG2.13\u201114",
                "BG2.13-2.14",
                "SB1.2.6–7",
                "CCAdi1.1–2"
            };
            foreach (string reference in references)
            {
                Check(Core.IsScriptureReference(reference), "whole scripture syntax " + reference);
                Equal(null, c.Trigger(reference), "bare reference never expands during typing " + reference);
                Equal(reference, CaretWords.AtEnd("Earlier untouched " + reference), "pasted reference remains whole " + reference);
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string trigger = reference + (mode == "balaram" ? "<" : mode == "plain" ? "?" : ">");
                    string explicitOutput = c.Trigger(trigger), output = c.CtrlExpansion(reference, mode);
                    Check(!string.IsNullOrEmpty(output), "Ctrl scripture found " + reference + " " + mode);
                    Equal(explicitOutput, output, "Ctrl matches explicit scripture " + trigger);
                    Equal(output, c.ConvertHighlighted(reference, mode), "selected reference matches caret " + reference + " " + mode);
                    Equal(string.Join("|", c.TriggerItalics(trigger, output, mode).Select(x => x.Start + ":" + x.Length + ":" + x.Force)), string.Join("|", c.CtrlItalics(reference, output, mode).Select(x => x.Start + ":" + x.Length + ":" + x.Force)), "verse/prose formatting matches " + trigger);
                }

                foreach (string prefix in new[]
                {
                    "",
                    "Earlier untouched ",
                    "Earlier\r\n",
                    "(Quote “"
                }

                )
                    foreach (string tail in new[]
                    {
                        "",
                        ",",
                        ").",
                        "”"
                    }

                    )
                    {
                        string text = prefix + reference + tail + " next";
                        for (int offset = 0; offset <= reference.Length; offset++)
                        {
                            int caret = prefix.Length + offset;
                            var span = CaretWords.Locate(text, caret, caret);
                            Check(span != null && span.Start == prefix.Length && span.End == prefix.Length + reference.Length && span.Word == reference, "entire reference at every caret position " + reference + " " + offset);
                        }

                        int after = prefix.Length + reference.Length + tail.Length;
                        var punctuation = CaretWords.Locate(text, after, after);
                        Check(punctuation != null && punctuation.Word == reference && punctuation.Tail == tail, "punctuation kept outside reference " + reference + tail);
                    }
            }

            foreach (string mode in new[]
            {
                "unicode",
                "balaram",
                "plain"
            }

            )
            {
                string verse = c.CtrlExpansion("BG18.66", mode), combined = c.CtrlExpansion("BG18.66VT", mode);
                var forced = c.CtrlItalics("BG18.66VT", combined, mode).Where(x => x.Force).ToArray();
                Check(forced.Length > 0 && forced.All(x => x.Start + x.Length <= verse.Length), "complete verse forced italic, translation not forced " + mode);
                Check(!c.CtrlItalics("BG18.66T", c.CtrlExpansion("BG18.66T", mode), mode).Any(x => x.Force), "translation keeps prose rules " + mode);
                Check(c.CaretChoices("BG18.66VT", mode).Length == 0, "scripture not a spelling cycle " + mode);
            }

            Equal(c.CtrlExpansion("BG18.66VT", "unicode"), c.CtrlExpansion("bg18.66vt", "unicode"), "existing scripture case handling retained");
            foreach (string invalid in new[]
            {
                "BG",
                "BG18.",
                "BG18.66.1.1",
                "BG18.66VTmore",
                "XBG18.66",
                "BG18.66.pdf",
                "reader@BG18.66",
                "https://example.com/BG18.66",
                "C:\\temp\\BG18.66",
                "BG18.66/name",
                "BG18.66_name",
                "#BG18.66",
                "BG18.66 next",
                "24",
                "24th",
                "age",
                "SP!"
            }

            )
            {
                Check(!Core.IsScriptureReference(invalid), "non-reference syntax rejected " + invalid);
                Equal(c.CtrlShort(invalid, "unicode"), c.CtrlExpansion(invalid, "unicode"), "non-reference preserves configured abbreviation behavior " + invalid);
            }

            Equal(null, c.CtrlExpansion("BG999.999VT", "unicode"), "missing record leaves reference alone");
            Equal("Read BG18.66 and Kṛṣṇa.", c.ConvertHighlighted("Read BG18.66 and Krsna.", "unicode"), "passage conversion keeps citations as references");
            foreach (string mode in new[]
            {
                "auto",
                "font-auto",
                "devanagari",
                "bengali",
                "itrans",
                null
            }

            )
                Equal(null, c.CtrlExpansion("BG18.66", mode), "unsupported scripture mode leaves text alone");
            string spaced = "Earlier BG18.66 ";
            Check(CaretWords.Locate(spaced, spaced.Length, spaced.Length) == null, "no insertion after a trailing space");
            Check(CaretWords.Locate("BG18.66", 0, 7) == null, "caret targeting never consumes a selection");
        }

        static void ScriptureRanges(Core c)
        {
            foreach (string mode in new[]
            {
                "unicode",
                "balaram",
                "plain"
            }

            )
            {
                string suffix = mode == "balaram" ? "<" : mode == "plain" ? "?" : ">";
                string first = c.Trigger("BG2.13" + suffix), second = c.Trigger("BG2.14" + suffix), firstT = c.Trigger("BG2.13T" + suffix), secondT = c.Trigger("BG2.14T" + suffix);
                string verses = first + "\n\n" + second, paired = first + "\n\n" + firstT + "\n\n" + second + "\n\n" + secondT;
                foreach (string dash in new[]
                {
                    "-",
                    "\u2011",
                    "–"
                }

                )
                {
                    string reference = "BG2.13" + dash + "14";
                    Equal(verses, c.CtrlExpansion(reference, mode), "bare range inserts only verses " + mode + dash);
                    Equal(verses, c.Trigger(reference + suffix), "range explicit symbol " + mode + dash);
                    Equal(verses, c.CtrlExpansion(reference + "V", mode), "V range matches default " + mode + dash);
                    Equal(firstT + "\n\n" + secondT, c.CtrlExpansion(reference + "T", mode), "T range inserts translations " + mode + dash);
                    Equal(paired, c.CtrlExpansion(reference + "VT", mode), "each verse paired with its translation " + mode + dash);
                    string title = Core.Target("Bhagavad-gītā", mode) + " 2.13" + dash + "14";
                    Equal(verses + "\n\n" + title, c.CtrlExpansion(reference + "R", mode), "R adds one range reference to verses " + mode + dash);
                    Equal(paired + "\n\n" + title, c.CtrlExpansion(reference + "VTR", mode), "VTR adds one final range reference " + mode + dash);
                    var spans = c.CtrlItalics(reference + "VT", paired, mode).Where(x => x.Force).ToArray();
                    int secondStart = first.Length + firstT.Length + 4;
                    Check(spans.Any(x => x.Start >= secondStart) && spans.All(x => x.Start + x.Length <= first.Length || x.Start >= secondStart && x.Start + x.Length <= secondStart + second.Length), "both verses forced italic, neither translation forced " + mode + dash);
                    var state = new InputState();
                    string captured = null;
                    foreach (char character in "Earlier untouched " + reference + "VT" + suffix)
                    {
                        string candidate = state.Type(character);
                        if (candidate != null)
                            captured = candidate;
                    }

                    Equal(reference + "VT" + suffix, captured, "typed range captured whole " + mode + dash);
                }

                Equal(verses, c.CtrlExpansion("BG2.13-2.14", mode), "full endpoint in same chapter " + mode);
                Equal(c.CtrlExpansion("CCAdi1.1–2", mode), c.CtrlExpansion("CCAdi1.1–1.2", mode), "named-section endpoint retains chapter " + mode);
                string shared = c.Trigger("BG2.42T" + suffix), sharedExpected = c.Trigger("BG2.42" + suffix) + "\n\n" + c.Trigger("BG2.43" + suffix) + "\n\n" + shared;
                Equal(shared, c.Trigger("BG2.43T" + suffix), "fixture has a shared translation " + mode);
                Equal(sharedExpected, c.CtrlExpansion("BG2.42-43VT", mode), "shared translation appears once after its verses " + mode);
                Equal(shared, c.CtrlExpansion("BG2.42-43T", mode), "shared translation-only range not duplicated " + mode);
            }

            foreach (string invalid in new[]
            {
                "BG2.14-13",
                "BG2.0-2",
                "BG2.13-3.1",
                "BG2.1-201",
                "BG2.13--14",
                "CC1.2.3-1.3.4",
                "BG2.13-14.2"
            }

            )
            {
                Check(!Core.IsScriptureReference(invalid), "invalid range not a caret target " + invalid);
                Equal(null, c.CtrlExpansion(invalid, "unicode"), "invalid range leaves text unchanged " + invalid);
                Equal(null, c.Trigger(invalid + ">"), "invalid typed range leaves text unchanged " + invalid);
            }

            Equal(null, c.CtrlExpansion("BG18.78-79VT", "unicode"), "missing last record inserts no partial range");
        }

        public static int Main(string[] args)
        {
            AcbspUpgrade();
            SsrAddition(args[0]);
            BbAddition(args[0]);
            using (Core c = new Core(args[0]))
            {
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string title = "The Science of Self-Realization";
                    Equal(title, c.CtrlExpansion("SSR", mode), "SSR Ctrl output " + mode);
                    Equal(title, c.ConvertHighlighted("SSR", mode), "selected SSR output " + mode);
                    Equal(title, c.Trigger("SSR" + (mode == "unicode" ? ">" : mode == "balaram" ? "<" : "?")), "SSR explicit output " + mode);
                }

                Equal(null, c.Trigger("SSR"), "bare SSR waits for Ctrl");
                Equal(null, c.CtrlShort("ssr", "unicode"), "SSR remains case-sensitive");
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string expected = Core.Target("A.C. Bhaktivedanta Swami Prabhupāda", mode);
                    Equal(expected, c.CtrlShort("ACBSP", mode), "ACBSP initials have no internal space " + mode);
                    Equal(expected, c.Trigger("ACBSP" + (mode == "unicode" ? ">" : mode == "balaram" ? "<" : "?")), "ACBSP explicit output " + mode);
                }

                var active = c.ActiveShortTriggers();
                foreach (var row in active)
                {
                    if (row.Key.Length < 2)
                        continue;
                    char suffix = row.Key[row.Key.Length - 1];
                    if (suffix != '>' && suffix != '<' && suffix != '?')
                        continue;
                    string stem = row.Key.Substring(0, row.Key.Length - 1);
                    if (!Core.CtrlOnlyShort(stem))
                        continue;
                    string mode = suffix == '<' ? "balaram" : suffix == '?' ? "plain" : "unicode";
                    Equal(Configuration.Expand(row.Value, DateTime.Now), c.CtrlShort(stem, mode), "existing YAML Ctrl alias " + row.Key);
                    Equal(c.Trigger(row.Key), c.CtrlShort(stem, mode), "explicit trigger preserved " + row.Key);
                    Equal(c.CtrlShort(stem, mode), c.CtrlExpansion(stem, mode), "configured priority preserved " + row.Key);
                }

                Equal(null, c.CtrlShort("sp", "unicode"), "case sensitive");
                Equal(null, c.CtrlShort("SPG extra", "unicode"), "whole selection exact");
                Equal(null, c.CtrlShort("SP!", "unicode"), "no punctuation match");
                Equal(null, c.CtrlShort("age", "unicode"), "English no expansion");
                Equal(c.Word("raja", "unicode"), c.ConvertHighlighted("raja", "unicode"), "ordinary word fallback respects current preference");
                ScriptureCtrl(c);
                ScriptureRanges(c);
            }

            string root = Path.Combine(Path.GetDirectoryName(typeof(CtrlShortTests).Assembly.Location), "ctrl-short-fixture");
            Directory.CreateDirectory(Path.Combine(root, "data"));
            Directory.CreateDirectory(Configuration.Folder(root));
            foreach (string file in Directory.GetFiles(Path.Combine(args[0], "data")))
                if (!File.Exists(Path.Combine(root, "data", Path.GetFileName(file))))
                    File.Copy(file, Path.Combine(root, "data", Path.GetFileName(file)));
            Configuration.Write(Configuration.Short(root), new Dictionary<string, string> { { "SP>", "Śrīla Prabhupāda" }, { "SP<", "Çréla Prabhupäda" }, { "SP?", "Srila Prabhupada" }, { "SPG", "Supreme Personality of Godhead" }, { "KJ", "Kṛṣṇa" }, { "ZZ>", "Custom exact output" }, { "NOI1>", "Configured scripture override" } });
            using (Core c = new Core(root))
            {
                Equal("Configured scripture override", c.CtrlExpansion("NOI1", "unicode"), "configured abbreviation wins over scripture");
                Equal("Śrīla Prabhupāda", c.CtrlShort("SP", "unicode"), "Unicode SP");
                Equal("Çréla Prabhupäda", c.CtrlShort("SP", "balaram"), "Balaram SP");
                Equal("Srila Prabhupada", c.CtrlShort("SP", "plain"), "plain SP");
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                    Equal("Supreme Personality of Godhead", c.CtrlShort("SPG", mode), "single roman replacement " + mode);
                Equal("kåñëa", c.CtrlShort("KJ", "balaram").ToLowerInvariant(), "single Unicode replacement encodes");
                Equal("Custom exact output", c.ConvertHighlighted("ZZ", "unicode"), "selected alias precedence");
                Equal(null, c.CtrlShort("ZZ", "balaram"), "deleted variant not inferred");
                Equal(null, c.Trigger("SPG"), "bare trigger requires Ctrl");
                Equal("False", c.IsDirectTrigger("SPG").ToString(), "bare not automatic");
                Equal("True", c.IsDirectTrigger("SP>").ToString(), "suffix automatic");
            }

            var span = CaretWords.Locate("Earlier untouched SPG", 21, 21);
            Equal("SPG", span == null ? null : span.Word, "current token boundary");
            var number = CaretWords.Locate("4REGS", 5, 5);
            Equal("4REGS", number == null ? null : number.Word, "numeric abbreviation");
            var ctrl = new LeftControlTap(); // Existing modifier guard is exercised by the font-cycling suite.
            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
