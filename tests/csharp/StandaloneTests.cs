// Noninteractive regressions: configuration compatibility, strict parsing, reload safety, and shared conversion plans.
// Run: .\test.ps1 -Suite StandaloneTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace KeyJaya
{
    public static class StandaloneTests
    {
        static int passed, failed;
        static void Check(bool value, string name)
        {
            if (value)
                passed++;
            else
            {
                failed++;
                if (failed < 50)
                    Console.WriteLine("FAIL " + name);
            }
        }

        static void Equal(string a, string b, string name)
        {
            Check(a == b, name + " expected=" + a + " actual=" + b);
        }

        static void Reject(string path, string text)
        {
            File.WriteAllText(path, text, new UTF8Encoding(false));
            try
            {
                Configuration.Read(path);
                Check(false, "invalid configuration accepted " + text);
            }
            catch (InvalidDataException)
            {
                passed++;
            }
        }

        static string Route(Core c, string input)
        {
            InputState s = new InputState();
            string output = null;
            foreach (char x in input)
            {
                string trigger = s.Type(x, c);
                if (trigger != null)
                    output = c.Trigger(trigger) ?? output;
            }

            return output;
        }

        static void ConsolidatedCustomizations(string source, string scratch)
        {
            string root = Path.Combine(scratch, "customizations"), folder = Configuration.Folder(root);
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Path.Combine(root, "data"));
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            foreach (string data in Directory.GetFiles(Path.Combine(source, "data")))
                File.Copy(data, Path.Combine(root, "data", Path.GetFileName(data)));
            File.Copy(Configuration.Factory(source), Configuration.Factory(root));
            Configuration.CreateDefaultTriggers(root);
            Check(File.ReadAllBytes(Configuration.Short(root)).SequenceEqual(File.ReadAllBytes(Configuration.Factory(root))), "fresh defaults come directly from factory");
            Configuration.Write(Configuration.Short(root), new Dictionary<string, string>());
            byte[] empty = File.ReadAllBytes(Configuration.Short(root));
            Configuration.CreateDefaultTriggers(root);
            Check(File.ReadAllBytes(Configuration.Short(root)).SequenceEqual(empty), "creating defaults never resurrects deleted triggers");
            var tables = new Dictionary<string, string>
            {
                {
                    "diacritics-overrides.tsv",
                    "testalias\tśāstra\n"
                },
                {
                    "overrides.tsv",
                    "testalias\tśikhā\n"
                },
                {
                    "word-spellings.tsv",
                    "raja\trājā\nage\t!keep\n"
                },
                {
                    "script-spellings.tsv",
                    "kala\tकला\tকলা\n"
                },
                {
                    "script-phrases.tsv",
                    "kala rang\tकाला रंग\tকালো রং\n"
                },
                {
                    "italics-exceptions.tsv",
                    "raja\troman\nbhakti\titalic\n"
                },
                {
                    "conversion-rules.tsv",
                    "ZZZ\tYYY\nYYY\tXXX\n"
                }
            };
            foreach (var row in tables)
                File.WriteAllText(Path.Combine(folder, row.Key), row.Value, new UTF8Encoding(true));
            var before = Directory.GetFiles(folder).ToDictionary(p => p, File.ReadAllBytes);
            string unified = Path.Combine(folder, Customizations.FileName);
            string sections = "[aliases]\ntestalias\tśikhā\n[words]\nraja\trājā\nage\t!keep\n[script-words]\nkala\tकला\tকলা\n[script-phrases]\nkala rang\tकाला रंग\tকালো রং\n[italics]\nraja\troman\nbhakti\titalic\n[replacements]\nZZZ\tYYY\nYYY\tXXX\n";
            var samples = new[]
            {
                "Earlier age raja and Krsna",
                "testalias",
                "kala",
                "kala rang",
                "ZZZ",
                "āyurveda",
                "niti-sastra",
                "kari'",
                "çakti",
                "श्रीकृष्णाय"
            };
            var baseline = new Dictionary<string, string>();
            using (Core legacy = new Core(root))
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain",
                    "itrans",
                    "devanagari",
                    "bengali",
                    "hindi"
                }

                )
                    foreach (string input in samples)
                    {
                        baseline[mode + input] = legacy.ConvertSource(input, mode);
                        baseline["word" + mode + input] = legacy.ConvertHighlighted(input, mode);
                    }

            // Keep the original legacy bytes as evidence; remove only fixture-owned files.
            foreach (string name in tables.Keys)
                File.Move(Path.Combine(folder, name), Path.Combine(folder, name + ".evidence"));
            File.WriteAllText(unified, sections, new UTF8Encoding(true));
            byte[] modern = File.ReadAllBytes(unified);
            using (Core current = new Core(root))
            {
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain",
                    "itrans",
                    "devanagari",
                    "bengali",
                    "hindi"
                }

                )
                    foreach (string input in samples)
                    {
                        Equal(baseline[mode + input], current.ConvertSource(input, mode), "legacy/unified copied equivalence " + mode + input);
                        Equal(baseline["word" + mode + input], current.ConvertHighlighted(input, mode), "legacy/unified Ctrl equivalence " + mode + input);
                    }

                Equal("śikhā", current.Word("testalias", "unicode"), "legacy alias priority retained");
                Equal("XXX", current.ConvertSource("ZZZ", "unicode"), "ordered replacements retained");
                Equal("", string.Join("", current.ItalicWords("rājā", "unicode").Select(x => "italic")), "unified roman exception");
                Check(current.ItalicWords("bhakti", "unicode").Length > 0, "unified italic exception");
            }

            Check(File.ReadAllBytes(unified).SequenceEqual(modern), "unified configuration never rewritten while loading");
            File.WriteAllText(unified, sections + "[old]\t[new]\n[new]\tFinal\n[omit]\t\n", new UTF8Encoding(false));
            using (Core brackets = new Core(root))
            {
                Equal("Final", brackets.ConvertSource("[old]", "unicode"), "literal bracket replacements are rows, not section headers");
                Equal("", brackets.ConvertSource("[omit]", "unicode"), "bracket replacement may have an empty output");
            }

            foreach (var row in tables)
                File.Copy(Path.Combine(folder, row.Key + ".evidence"), Path.Combine(folder, row.Key));
            File.WriteAllText(unified, "[words]\nraja\t!keep\nage\tāge\n[aliases]\ntestalias\tśāstra\n[script-words]\nkala\tकाल\tকাল\n[script-phrases]\nkala rang\tकला रंग\tকলা রং\n[italics]\nraja\titalic\n", new UTF8Encoding(false));
            using (Core overlay = new Core(root))
            {
                Equal("raja", overlay.Word("raja", "unicode"), "unified keep overrides old preferred spelling");
                Equal("āge", overlay.Word("age", "unicode"), "unified preference overrides old keep");
                Equal("śāstra", overlay.Word("testalias", "unicode"), "unified alias overrides old alias");
                Equal("काल", overlay.ConvertSource("kala", "devanagari"), "unified script word overrides old script word");
                Equal("কলা রং", overlay.ConvertSource("kala rang", "bengali"), "unified phrase overrides old phrase");
                Check(overlay.ItalicWords("rājā", "unicode").Length > 0, "unified style overrides old style");
            }

            foreach (var row in before)
                Check(File.ReadAllBytes(row.Key).SequenceEqual(row.Value), "all original legacy settings remain byte-identical " + Path.GetFileName(row.Key));
            // Identical FROM keys must override in place, not run after the old
            // rule has consumed its input. Unrelated rules retain their order.
            string rulePath = Path.Combine(folder, "conversion-rules.tsv");
            File.WriteAllText(rulePath, "ZZZ\tYYY\nBBB\tZZZ\nZZZ\tOLD\nDDD\tEEE\n", new UTF8Encoding(true));
            File.WriteAllText(unified, "[replacements]\nZZZ\tXXX\nEEE\tFFF\n", new UTF8Encoding(false));
            byte[] legacyRules = File.ReadAllBytes(rulePath), unifiedRules = File.ReadAllBytes(unified);
            using (Core merged = new Core(root))
            {
                Equal("XXX", merged.Convert("ZZZ", "unicode"), "unified replacement wins conflicting legacy FROM");
                Equal("ZZZ", merged.Convert("BBB", "unicode"), "override retains first legacy position without a stale duplicate afterward");
                Equal("FFF", merged.Convert("DDD", "unicode"), "new unified replacement appends after unrelated legacy rules");
                Equal("zzz", merged.Convert("zzz", "unicode"), "replacement keys remain case sensitive");
                Equal("XXX", merged.ConvertSource("ZZZ", "unicode"), "copy conversion observes unified rule precedence");
            }

            Check(File.ReadAllBytes(rulePath).SequenceEqual(legacyRules) && File.ReadAllBytes(unified).SequenceEqual(unifiedRules), "replacement merge never rewrites either settings file");
            File.WriteAllText(unified, "[replacements]\nZZZ\t\n", new UTF8Encoding(false));
            using (Core deletion = new Core(root))
                Equal("", deletion.Convert("ZZZ", "unicode"), "empty unified output overrides a legacy replacement");
            File.Copy(rulePath + ".evidence", rulePath, true);
            foreach (string bad in new[]
            {
                "raja\trāja",
                "[unknown]\n",
                "[words]\n[words]\n",
                "[aliases]\nmissing tab\n",
                "[words]\nraja\trāja\nRAJA\trājā\n",
                "[script-words]\nx\twrong\t\n",
                "[script-phrases]\none\tक\tক\n",
                "[italics]\nraja\twrong\n",
                "[replacements]\n\tvalue\n"
            }

            )
            {
                File.WriteAllText(unified, bad, new UTF8Encoding(false));
                try
                {
                    using (Core invalid = new Core(root))
                    {
                    }

                    Check(false, "invalid unified customization accepted");
                }
                catch (InvalidDataException)
                {
                    passed++;
                }
            }

            File.WriteAllText(unified, sections, new UTF8Encoding(false));
            using (Core valid = new Core(root))
            {
                File.WriteAllText(unified, "[unknown]\n");
                try
                {
                    Core.Reload(valid, root);
                    Check(false, "invalid unified reload accepted");
                }
                catch (InvalidDataException)
                {
                    passed++;
                }

                Equal("śikhā", valid.Word("testalias", "unicode"), "failed unified reload retains working engine");
            }

            Directory.Move(folder, folder + ".evidence");
            using (Core fallback = new Core(root))
            {
                Equal("Bṛhad-bhāgavatāmṛta", fallback.CtrlExpansion("BB", "unicode"), "fallback uses same complete factory list");
                Equal("International Society for Krishna Consciousness", fallback.CtrlExpansion("ISKCON", "unicode"), "fallback preserves exact replacement metadata");
                Check(fallback.ActiveShortTriggers().Count == Configuration.Read(Configuration.Factory(root)).Count, "fallback includes every factory trigger");
            }

            Directory.Move(folder + ".evidence", folder);
        }

        static void SharedConversionPlans(string source)
        {
            using (Core core = new Core(source))
            {
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                    foreach (string input in new[]
                    {
                        "raja",
                        "rāja",
                        "ācāryas",
                        "Vyasa-puja",
                        "God/Krsna",
                        "Earlier raja and Krsna",
                        "an age since",
                        "SP",
                        "ISKCON",
                        "SSR",
                        "BB",
                        "BG2.1-3VT",
                        "24",
                        "24th"
                    }

                    )
                    {
                        var plan = core.PrepareWordConversion(input, mode);
                        Equal(plan.Text ?? core.ConvertCopied(input, mode), core.ConvertHighlighted(input, mode), "shared Ctrl plan " + input + mode);
                        if (plan.Expansion)
                        {
                            Check(plan.Choices.Length == 0, "expansions never cycle");
                            Equal(core.CtrlExpansion(input, mode), plan.Text, "configured/scripture priority preserved");
                        }
                    }

                Equal("Goswami", core.PrepareWordConversion("Gosvāmī", "unicode", "Tamāla Kṛṣṇa Gosvāmī").Text, "shared modern name context");
                Equal("Gosvāmī", core.PrepareWordConversion("Goswami", "unicode", "Rūpa Goswami").Text, "shared historical name context");
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain",
                    "itrans",
                    "devanagari",
                    "bengali",
                    "hindi"
                }

                )
                    foreach (string input in new[]
                    {
                        "kRShNa",
                        "Earlier raja and Krsna",
                        "kåñëa",
                        "कृष्ण",
                        "কৃষ্ণ"
                    }

                    )
                    {
                        Equal(core.ConvertCopied(input, mode), core.ConvertSource(input, mode), "shared auto-source dispatch");
                        Equal(core.ConvertBalaram(input, mode), core.ConvertSource(input, mode, "balaram"), "shared legacy-source dispatch");
                        Equal(core.ConvertItrans(input, mode), core.ConvertSource(input, mode, "itrans"), "shared ITRANS-source dispatch");
                    }

                var choices = new[]
                {
                    "rāja",
                    "rājā",
                    "raja"
                };
                string current = "raja";
                for (int n = 0; n < 300; n++)
                {
                    current = Core.NextSpelling(current, choices);
                    Equal(choices[n % 3], current, "shared continuous cycle " + n);
                }

                Equal(null, Core.NextSpelling("age", new string[0]), "unknown words do not cycle");
            }
        }

        [STAThread]
        public static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            string scratch = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])), "standalone-test-artifacts", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            string file = Path.Combine(scratch, "personal.yml");
            ConsolidatedCustomizations(args[0], scratch);
            SharedConversionPlans(args[0]);
            File.WriteAllText(file, "matches:\n  - trigger: ':quote'\n    replace: 'It''s nice'\n  - trigger: ':sign'\n    replace: |-\n      Best wishes,\n      Your name\n  - trigger: ':fold'\n    replace: >-\n      first\n      second\n  - trigger: ':date'\n    replace: '{{date:%Y-%m-%d}}'\n", new UTF8Encoding(false));
            var parsed = Configuration.Read(file);
            Equal("It's nice", parsed[":quote"], "apostrophe");
            Equal("Best wishes,\nYour name", parsed[":sign"], "multiline");
            Equal("first second", parsed[":fold"], "folded");
            Equal("2026-10-11", Configuration.Expand(parsed[":date"], new DateTime(2026, 10, 11)), "date");
            Equal("2026 26 10 11 13 01 14 15 PM Oct October Sun Sunday %", Configuration.Date("%Y %y %m %d %H %I %M %S %p %b %B %a %A %%", new DateTime(2026, 10, 11, 13, 14, 15)), "date formats");
            Random random = new Random(170);
            for (int i = 0; i < 1000; i++)
            {
                string value = "Unicode rājä \\\" ' & < >\t\n" + random.Next() + " # literal";
                var rows = new Dictionary<string, string>
                {
                    {
                        ":test" + i,
                        value
                    }
                };
                Configuration.Write(file, rows);
                Equal(value, Configuration.Read(file)[":test" + i], "roundtrip escaped configuration " + i);
            }

            foreach (string bad in new[]
            {
                "",
                "trigger: ':x'\nreplace: x",
                "matches:\n - trigger: ':x'\n   replace: x\n - trigger: ':x'\n   replace: y",
                "matches:\n - trigger: 'two words'\n   replace: x",
                "matches:\n - trigger: ':x'\n   replace: '{{output}}'",
                "matches:\n - trigger: ':x'\n   replace: '{{date:%Q}}'",
                "matches:\n - trigger: ':x'\n   replace: x\n   word: true",
                "matches:\n - trigger: ':x'\n   replace: x\n   vars:\n     - name: command",
                "matches:\n - trigger: ':x'\n   replace: \"broken",
                "matches:\n - trigger: ':x'",
                "matches: []\nmatches: []"
            }

            )
                Reject(file, bad);
            var all = new Dictionary<string, string>
            {
                {
                    ":hello",
                    "Hello!"
                },
                {
                    ":Hello",
                    "Different personal replacement"
                },
                {
                    ":sign",
                    "First line\nSecond line"
                },
                {
                    "HT>",
                    "Custom Haridāsa"
                }
            };
            Configuration.Write(file, all);
            string root = Path.Combine(scratch, "app");
            Directory.CreateDirectory(Configuration.Folder(root));
            Directory.CreateDirectory(Path.Combine(root, "state"));
            Directory.CreateDirectory(Path.Combine(root, "data"));
            foreach (string data in Directory.GetFiles(Path.Combine(args[0], "data")))
                File.Copy(data, Path.Combine(root, "data", Path.GetFileName(data)));
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            File.Copy(Configuration.Factory(args[0]), Configuration.Factory(root));
            File.Copy(file, Configuration.Personal(root));
            using (Core c = new Core(root))
            {
                Equal("Hello!", c.TriggerWithObservedCase(":hello", ":Hello"), "Word capitalization preserves the physically typed personal trigger");
                Equal("Custom Haridāsa", c.TriggerWithObservedCase("HT>", "Ht>"), "Word capitalization preserves short-trigger literal replacement");
                Equal("Hello!", Route(c, "Earlier an age since :hello"), "colon trigger full token");
                Equal("First line\nSecond line", Route(c, ":sign"), "multiline trigger");
                Equal("Custom Haridāsa", Route(c, "HT>"), "user override precedence");
                Check(c.IsDirectTrigger("HT>"), "short trigger direct ownership");
                Equal(null, Route(c, "x:hello"), "no suffix expansion within preceding word");
                Equal(null, Route(c, "an> age> since>"), "English untouched");
                Equal(null, Route(c, "Earlier 123#"), "full number token");
                Equal(null, Route(c, "Earlier 123rd#"), "ordinal trigger");
                Equal(null, Route(c, "1,234#"), "comma number");
                Equal(null, Route(c, "x123#"), "embedded number untouched");
                Equal(null, Route(c, "12,34#"), "bad grouped number untouched");
                Equal(null, Route(c, "1.5#"), "decimal untouched");
                Equal(null, Route(c, "-1#"), "negative untouched");
                Equal(null, Route(c, new string ('x', 161) + ":hello"), "overflow no suffix editing");
                for (int n = 0; n < 10000; n++)
                {
                    string value = n.ToString();
                    Equal(null, Route(c, "prefix an age since " + value + "#"), "retired number route " + value);
                    Equal(null, Route(c, value + "th#"), "retired ordinal route " + value);
                    Equal(Numbers.Words(value, false), c.ConvertHighlighted(value, "unicode"), "Ctrl number " + value);
                    Equal(Numbers.Words(value, true), c.ConvertHighlighted(value + "th", "unicode"), "Ctrl ordinal " + value);
                }

                foreach (string raw in new[]
                {
                    "18446744073709551616#",
                    "1,00#",
                    "0,0000#",
                    "12,345,67#",
                    "foo12#",
                    "#",
                    "1st:",
                    "1 : :",
                    "1\n#"
                }

                )
                    Equal(null, Route(c, raw), "invalid numeric ownership " + raw);
                c.RememberSpelling("rāja", "räjä", "balaram", "auto");
                c.FinishSpellingChoice();
                Check(File.Exists(Path.Combine(root, "state", "preferred-spellings.tsv")), "preferences in state folder");
                Reject(Configuration.Personal(root), "matches:\n - trigger: ':bad'\n   replace: '{{script}}'");
                try
                {
                    using (Core replacement = Core.Reload(c, root))
                    {
                    }

                    Check(false, "invalid reload accepted");
                }
                catch (InvalidDataException)
                {
                    passed++;
                }

                Equal("Hello!", c.Trigger(":hello"), "invalid reload leaves active engine working");
                File.Move(Configuration.Personal(root), Configuration.Personal(root) + ".bad");
                try
                {
                    using (Core replacement = Core.Reload(c, root))
                    {
                    }

                    Check(false, "missing config accepted");
                }
                catch (InvalidDataException)
                {
                    passed++;
                }

                Equal("Hello!", c.Trigger(":hello"), "missing config preserves active engine");
                all[":hello"] = "Changed!";
                Configuration.Write(Configuration.Personal(root), all);
                c.RememberSpelling("rāja", "räjä", "balaram", "auto");
                c.FinishSpellingChoice();
                c.RememberSpelling("rāja", "räjä", "balaram", "auto");
                using (Core replacement = Core.Reload(c, root))
                {
                    Equal("Changed!", replacement.Trigger(":hello"), "valid reload applies exact edited trigger");
                    Equal("rājā", replacement.Convert("rāja", "unicode"), "reload retains pending third preference vote");
                }
            }

            InputState undo = new InputState();
            undo.RecordReplacement("prana>", "prāṇa", new IntPtr(7));
            Equal("prana>", undo.UndoOriginal, "explicit trigger retains undo");
            Equal("prāṇa", undo.UndoOutput, "explicit output ownership");
            undo.RecordReplacement("doesnt ", "doesn’t ", new IntPtr(7), true);
            Equal(null, undo.UndoOriginal, "auto contraction cannot revert");
            Equal(null, undo.UndoOutput, "no stale manual undo after correction");
            undo.RecordReplacement("räja", "rāja", new IntPtr(7));
            Equal("räja", undo.UndoOriginal, "clipboard conversion retains undo");
            Equal("zero", Numbers.Single("0"), "zero");
            Equal("zeroth", Numbers.Single("0th"), "zeroth");
            Equal("twenty-first", Numbers.Single("21st"), "twenty first");
            Equal("one million", Numbers.Single("1,000,000"), "million");
            Equal("eighteen quintillion four hundred forty-six quadrillion seven hundred forty-four trillion seventy-three billion seven hundred nine million five hundred fifty-one thousand six hundred fifteen", Numbers.Single("18446744073709551615"), "maximum uint64");
            // The canonical YAML is authoritative: removing a built-in must not reveal a hidden default.
            string editable = Path.Combine(scratch, "editable");
            Directory.CreateDirectory(Configuration.Folder(editable));
            Directory.CreateDirectory(Path.Combine(editable, "data"));
            Directory.CreateDirectory(Path.Combine(editable, "state"));
            foreach (string data in Directory.GetFiles(Path.Combine(args[0], "data")))
                File.Copy(data, Path.Combine(editable, "data", Path.GetFileName(data)));
            Configuration.Write(Path.Combine(Configuration.Folder(editable), Configuration.PersonalFile), new Dictionary<string, string> { { "HT>", "Legacy override" } });
            var editableRows = new Dictionary<string, string>
            {
                {
                    "HT>",
                    "Haridāsa Ṭhākura"
                },
                {
                    ":mine",
                    "My replacement"
                }
            };
            Configuration.Write(Configuration.Short(editable), editableRows);
            Equal(Configuration.Short(editable), Configuration.Personal(editable), "editor opens canonical short-trigger YAML");
            using (Core editableCore = new Core(editable))
            {
                Equal("Haridāsa Ṭhākura", editableCore.Trigger("HT>"), "editable built-in active");
                editableRows["HT>"] = "Changed short replacement";
                Configuration.Write(Configuration.Short(editable), editableRows);
                using (Core changed = Core.Reload(editableCore, editable))
                    Equal("Changed short replacement", changed.Trigger("HT>"), "edit applies after reload");
                editableRows.Remove("HT>");
                Configuration.Write(Configuration.Short(editable), editableRows);
                using (Core deleted = Core.Reload(editableCore, editable))
                {
                    Equal(null, deleted.Trigger("HT>"), "deletion disables built-in despite legacy override");
                    Equal("My replacement", deleted.Trigger(":mine"), "unrelated custom trigger preserved");
                }
            }

            using (Core restarted = new Core(editable))
                Equal(null, restarted.Trigger("HT>"), "deletion persists after restart");
            passed += DistributionTests.Run(Path.Combine(scratch, "distribution"), Path.Combine(args[0], "assets", DistributionFiles.Manifest));
            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
