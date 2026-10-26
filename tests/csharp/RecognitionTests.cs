// Noninteractive regressions: corpus recognition and high-confidence aliases without relaxing English protection.
// Run: .\test.ps1 -Suite RecognitionTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KeyJaya
{
    public static class RecognitionTests
    {
        static int passed, failed;
        static void Check(bool yes, string name)
        {
            if (yes)
                passed++;
            else
            {
                failed++;
                if (failed < 40)
                    Console.WriteLine("FAIL " + name);
            }
        }

        static void Equal(string wanted, string actual, string name)
        {
            Check(wanted == actual, name + " wanted=" + wanted + " actual=" + actual);
        }

        public static int Main(string[] args)
        {
            using (Core c = new Core(args[0]))
            {
                string[] plain =
                {
                    "Srila",
                    "sikha",
                    "Varuna",
                    "Sarartha",
                    "darsini",
                    "Krsna",
                    "Sruti",
                    "pusti",
                    "marga",
                    "Tulasi",
                    "asana",
                    "Yasoda",
                    "Purnima",
                    "Vaisnavas"
                };
                string[] iast =
                {
                    "Śrīla",
                    "śikhā",
                    "Varuṇa",
                    "Sārārtha",
                    "darśinī",
                    "Kṛṣṇa",
                    "Śruti",
                    "puṣṭi",
                    "mārga",
                    "Tulasī",
                    "āsana",
                    "Yaśodā",
                    "Pūrṇimā",
                    "Vaiṣṇavas"
                };
                for (int n = 0; n < plain.Length; n++)
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        string expected = Core.Target(iast[n], mode);
                        Equal(expected, c.ConvertCopied(plain[n], mode), "reported copied " + plain[n]);
                        Equal(expected, c.ConvertHighlighted(plain[n], mode), "reported Ctrl tap " + plain[n]);
                        Equal(expected, c.Trigger(plain[n] + (mode == "balaram" ? "<" : ">")), "reported suffix " + plain[n]);
                        Check(c.CaretChoices(plain[n], mode).Contains(expected), "reported cycling " + plain[n]);
                    }

                foreach (var term in Vocabulary.Terms)
                {
                    if (c.English(term.Key) && !Core.IndicLoanwords.Contains(term.Key))
                        continue;
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        Check(c.CaretChoices(term.Key, mode).Length > 0, "supplement cycle " + term.Key);
                        Equal(Core.Target(term.Key == "gosvami" ? "Gosvāmī" : term.Key == "gosvamipada" ? "Gosvāmīpāda" : term.Value, mode), c.ConvertCopied(term.Key, mode), "supplement copied " + term.Key);
                        Equal(Core.Target(term.Key == "gosvami" ? "Gosvāmī" : term.Key == "gosvamipada" ? "Gosvāmīpāda" : term.Value, mode), c.Trigger(term.Key + (mode == "balaram" ? "<" : ">")), "supplement trigger " + term.Key);
                    }
                }

                string encoded = "apsu agni-devatAzca tiSThantyataH nApsu mUtra-pUriSaM kuryAt na niSThIvan na vivasanaH snAyAd guhyo vA eSaH agni-retasya agneranatidAhAya";
                string expectedVerse = "apsu agni-devatāśca tiṣṭhantyataḥ nāpsu mūtra-pūriṣaṃ kuryāt na niṣṭhīvan na vivasanaḥ snāyād guhyo vā eṣaḥ agni-retasya agneranatidāhāya";
                Equal(expectedVerse, c.ConvertItrans(encoded, "unicode"), "reported strict ITRANS");
                Equal(expectedVerse.Replace('ṃ', 'ṁ'), c.ConvertCopied(encoded, "unicode"), "reported automatically detected ITRANS");
                Check(!c.ConvertCopied(encoded, "unicode").Any(char.IsUpper), "no encoded capitals remain");
                Equal(EncodingMap.Balaram(expectedVerse), c.ConvertItrans(encoded, "balaram"), "reported Balaram");
                Equal(EncodingMap.Ascii(expectedVerse), c.ConvertItrans(encoded, "plain"), "reported plain");
                string[, ] aliases =
                {
                    {
                        "A",
                        "ā"
                    },
                    {
                        "aa",
                        "ā"
                    },
                    {
                        "I",
                        "ī"
                    },
                    {
                        "ii",
                        "ī"
                    },
                    {
                        "U",
                        "ū"
                    },
                    {
                        "uu",
                        "ū"
                    },
                    {
                        "R",
                        "ṛ"
                    },
                    {
                        "Ri",
                        "ṛ"
                    },
                    {
                        "R^i",
                        "ṛ"
                    },
                    {
                        "RRi",
                        "ṛ"
                    },
                    {
                        "Rri",
                        "ṛ"
                    },
                    {
                        "RR",
                        "ṝ"
                    },
                    {
                        "RRI",
                        "ṝ"
                    },
                    {
                        "R^I",
                        "ṝ"
                    },
                    {
                        "L",
                        "ḷ"
                    },
                    {
                        "Li",
                        "ḷ"
                    },
                    {
                        "L^i",
                        "ḷ"
                    },
                    {
                        "LLi",
                        "ḷ"
                    },
                    {
                        "Lli",
                        "ḷ"
                    },
                    {
                        "LL",
                        "ḹ"
                    },
                    {
                        "LLI",
                        "ḹ"
                    },
                    {
                        "L^I",
                        "ḹ"
                    },
                    {
                        "ch",
                        "c"
                    },
                    {
                        "c",
                        "c"
                    },
                    {
                        "JN",
                        "ñ"
                    },
                    {
                        "dny",
                        "jñ"
                    },
                    {
                        "jny",
                        "jñ"
                    },
                    {
                        "jnj",
                        "jñ"
                    },
                    {
                        "ld",
                        "ḷ"
                    },
                    {
                        "ee",
                        "e"
                    },
                    {
                        "oo",
                        "o"
                    },
                    {
                        "Ch",
                        "ch"
                    },
                    {
                        "CH",
                        "ch"
                    },
                    {
                        "chh",
                        "ch"
                    },
                    {
                        "sh",
                        "ś"
                    },
                    {
                        "z",
                        "ś"
                    },
                    {
                        "Sh",
                        "ṣ"
                    },
                    {
                        "shh",
                        "ṣ"
                    },
                    {
                        "S",
                        "ṣ"
                    },
                    {
                        "~N",
                        "ṅ"
                    },
                    {
                        "N^",
                        "ṅ"
                    },
                    {
                        "~n",
                        "ñ"
                    },
                    {
                        "j~n",
                        "jñ"
                    },
                    {
                        "GY",
                        "jñ"
                    },
                    {
                        "x",
                        "kṣa"
                    },
                    {
                        "M",
                        "ṃ"
                    },
                    {
                        ".m",
                        "ṃ"
                    },
                    {
                        ".n",
                        "ṃ"
                    },
                    {
                        "H",
                        "ḥ"
                    },
                    {
                        ".a",
                        "’"
                    },
                    {
                        ".N",
                        "m̐"
                    },
                    {
                        "OM",
                        "oṃ"
                    },
                    {
                        "SRI",
                        "śrī"
                    }
                };
                for (int n = 0; n < aliases.GetLength(0); n++)
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram",
                        "plain"
                    }

                    )
                        Equal(Core.Target(aliases[n, 1], mode), c.ConvertItrans(aliases[n, 0], mode), "alternate code " + aliases[n, 0]);
                Equal("tat", c.ConvertItrans("tat.h", "unicode"), "halant marker omitted in Latin");
                Equal("An age since The child", c.ConvertCopied("An age since The child", "unicode"), "English title-case ITRANS guard");
                Equal("स", c.ConvertCopied("sa", "devanagari"), "reported sa");
                Equal("স", c.ConvertCopied("sa", "bengali"), "Bengali sa");
                var spans = c.ItalicWords("Earlier an age since Śrīla and Kṛṣṇa, vaiṣṇavas!", "unicode");
                Equal("", string.Join("|", spans.Select(x => "Earlier an age since Śrīla and Kṛṣṇa, vaiṣṇavas!".Substring(x.Start, x.Length))), "only Indic words italicized");
                Check(c.ItalicWords("an age since", "unicode").Length == 0, "English formatting untouched");
                Equal("räja", string.Join("|", c.ItalicWords("English räja", "balaram").Select(x => "English räja".Substring(x.Start, x.Length))), "Balaram italic recognition");
                Check(c.ItalicWords("https://example.com/rāja", "unicode").Length == 0, "URL formatting untouched");
                string mixed = "Hare Kṛṣṇa and an English hare";
                Equal("", string.Join("|", c.ItalicWords(mixed, "unicode").Select(x => mixed.Substring(x.Start, x.Length))), "mantra Hare distinct from English animal");
                Check(c.ItalicWords("kRShNa", "itrans").Length == 0, "ITRANS proper name stays roman");
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    string trigger = "BG1.1VT" + (mode == "unicode" ? ">" : mode == "balaram" ? "<" : "?");
                    string inserted = c.Trigger(trigger);
                    Check(inserted != null, "verse italic fixture exists");
                    int split = inserted.IndexOf("\n\n", StringComparison.Ordinal);
                    var italic = c.TriggerItalics(trigger, inserted, mode);
                    foreach (Match word in Regex.Matches(inserted.Substring(0, split), @"[\p{L}\p{M}]+"))
                        Check(italic.Any(x => x.Start == word.Index && x.Length == word.Length), "all verse words italicized " + mode);
                    foreach (var span in italic)
                        Check(inserted.Substring(span.Start, span.Length) != "said", "English translation word stays regular " + mode);
                }

                Equal("tête-à-tête", c.ConvertCopied("tête-à-tête", "unicode"), "foreign accents are not loose Roman aliases");
                Check(c.ItalicWords("tête-à-tête café naïve", "unicode").Length == 0, "foreign accents do not imply Indic italics");
                string rich = "English <&> Kṛṣṇa\nবাংলা";
                var richSpans = new[]
                {
                    new ItalicSpan(12, 5),
                    new ItalicSpan(18, 5)
                };
                byte[] payload = RichClipboard.Html(rich, richSpans);
                string header = Encoding.UTF8.GetString(payload);
                int begin = int.Parse(Regex.Match(header, @"StartFragment:(\d+)").Groups[1].Value), end = int.Parse(Regex.Match(header, @"EndFragment:(\d+)").Groups[1].Value);
                Equal(RichClipboard.Fragment(rich, richSpans), Encoding.UTF8.GetString(payload, begin, end - begin), "CF_HTML multibyte offsets");
                Check(header.Contains("English &lt;&amp;&gt; <i>Kṛṣṇa</i>\n<i>বাংলা</i>"), "only Indic spans carry HTML italics");
                Check(RichClipboard.Html("abc", new[] { new ItalicSpan(2, 2) }) == null, "invalid HTML range refused");
                string styled = "Kṛṣṇa in Vṛndāvana studies śāstra with guru and Vaiṣṇavas; Śikhā and mārga.";
                Equal("śāstra|Śikhā|mārga", string.Join("|", c.ItalicWords(styled, "unicode").Select(x => styled.Substring(x.Start, x.Length))), "BBT names and loans versus generic capital noun");
                foreach (string word in new[]
                {
                    "ashram",
                    "dharma",
                    "guru",
                    "karma",
                    "mantra",
                    "yoga",
                    "sādhu",
                    "Bhagavān",
                    "Brahman",
                    "Gosvāmīs",
                    "Māyāvāda",
                    "Māyāvādī",
                    "Kali-yuga",
                    "Paramātmā",
                    "Vaiṣṇava",
                    "Vedānta",
                    "Vedas",
                    "Gaura-pūrṇimā",
                    "Janmāṣṭamī",
                    "Ratha-yātrā",
                    "Kurukṣetra",
                    "Dhṛtarāṣṭra",
                    "Sañjaya",
                    "Pāṇḍu"
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
                        Check(c.ItalicWords(Core.Target(word, mode), mode).Length == 0, "roman BBT/BVT " + word + " " + mode);
                string compound = "aṣṭāṅga-yoga and karma";
                Equal("aṣṭāṅga-yoga", string.Join("|", c.ItalicWords(compound, "unicode").Select(x => compound.Substring(x.Start, x.Length))), "Sanskrit compound retains italic loan component");
                Check(c.ItalicWords("Śāstra", "unicode").Length == 1, "sentence capitalization does not imply a name");
                foreach (string input in new[]
                {
                    "0123456789",
                    "०१२३४५६७८९",
                    "০১২৩৪৫৬৭৮৯"
                }

                )
                {
                    Equal("०१२३४५६७८९", c.ConvertCopied(input, "devanagari"), "Devanagari numeral output");
                    Equal("०१२३४५६७८९", c.ConvertCopied(input, "hindi"), "Hindi numeral output");
                    Equal("০১২৩৪৫৬৭৮৯", c.ConvertCopied(input, "bengali"), "Bengali numeral output");
                }

                Equal("१२.५, -४२ / ७%", c.ConvertCopied("12.5, -42 / 7%", "devanagari"), "Devanagari numeric punctuation");
                Equal("১২.৫, -৪২ / ৭%", c.ConvertCopied("12.5, -42 / 7%", "bengali"), "Bengali numeric punctuation");
                Equal("123", c.ConvertCopied("123", "unicode"), "Roman numbers unchanged");
                foreach (string input in new[]
                {
                    "raj",
                    "Raj"
                }

                )
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        string marked = input == "Raj" ? "Rāja" : "rāja", expected = mode == "balaram" ? EncodingMap.Balaram(marked) : marked;
                        Equal(expected, c.Word(input, mode), "raj word alias " + input + " " + mode);
                        Equal(expected, c.ConvertCopied(input, mode), "raj copied alias " + input + " " + mode);
                        Equal(expected, c.ConvertHighlighted(input, mode), "raj selected alias " + input + " " + mode);
                        Equal(expected, c.Trigger(input + (mode == "balaram" ? "<" : ">")), "raj trigger alias " + input + " " + mode);
                        Check(c.CaretChoices(input, mode).Contains(expected), "raj caret alias " + input + " " + mode);
                    }

                foreach (string mode in new[]
                {
                    "hindi",
                    "devanagari",
                    "bengali",
                    "itrans"
                }

                )
                {
                    Check(!Core.AutomaticItalicMode(mode), "script output cannot gain automatic italics " + mode);
                    Check(c.ItalicWords("śāstra mārga āsana शास्त्र मार्ग শাস্ত্র mArga", mode).Length == 0, "no script automatic italic spans " + mode);
                    string verse = c.Trigger("BG1.1V>");
                    Check(c.TriggerItalics("BG1.1V>", verse, mode).Length == 0, "script mode cannot force verse italics " + mode);
                }

                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                    Check(Core.AutomaticItalicMode(mode), "Roman automatic italics retained " + mode);
                string[] genericTerms =
                {
                    "śāstra",
                    "śikhā",
                    "mārga",
                    "āsana",
                    "varṇa",
                    "bhakti",
                    "puṣṭi"
                };
                foreach (int count in new[]
                {
                    6,
                    7,
                    12,
                    30,
                    100
                }

                )
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        string input = Core.Target(string.Join(" ", Enumerable.Range(0, count).Select(n => genericTerms[n % genericTerms.Length])), mode);
                        var italics = c.ItalicWords(input, mode);
                        Equal(count.ToString(), italics.Length.ToString(), "already marked long selection italics " + count + " " + mode);
                        Check(italics.All(x => x.Start >= 0 && x.Length > 0 && x.Start + x.Length <= input.Length), "long selection italic range bounds " + count + " " + mode);
                        Check(SelectionFormatting.Plan(input, input).Length == 0, "style-only selection needs no spelling patches " + count + " " + mode);
                    }

                foreach (string form in new[]
                {
                    "āsana",
                    "Āsana",
                    "āsanas",
                    "āsana’s"
                }

                )
                    foreach (string mode in new[]
                    {
                        "unicode",
                        "balaram"
                    }

                    )
                    {
                        string output = Core.Target(form, mode);
                        Check(c.ItalicWords(output, mode).Any(x => x.Start == 0 && x.Length == output.Length), "asana generic term receives italics " + form + " " + mode);
                    }

                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram"
                }

                )
                {
                    string suffix = mode == "unicode" ? ">" : "<", output = c.Trigger("asana" + suffix);
                    Check(output != null && c.TriggerItalics("asana" + suffix, output, mode).Any(x => x.Start == 0 && x.Length == output.Length), "asana trigger receives italics " + mode);
                }

                Check(c.ItalicWords("guru karma yoga", "unicode").Length == 0, "asana fix preserves English loanword exceptions");
                int recognized = 0;
                byte[] data = File.ReadAllBytes(Path.Combine(args[0], "data/conversion.dat"));
                using (BinaryReader rows = new BinaryReader(File.OpenRead(Path.Combine(args[0], "data/conversion.idx"))))
                    while (rows.BaseStream.Position < rows.BaseStream.Length)
                    {
                        rows.ReadUInt64();
                        long at = rows.ReadInt64();
                        int count = rows.ReadInt32();
                        string record = Encoding.UTF8.GetString(data, (int)at, count), key = record.Substring(0, record.IndexOf('\0'));
                        if (!Regex.IsMatch(key, @"^[a-z]+$") || c.English(key) && !Core.IndicLoanwords.Contains(key))
                            continue;
                        if (c.Word(key, "unicode") == key)
                            continue;
                        recognized++;
                        Check(c.CaretChoices(key, "unicode").Length > 0, "complete dictionary available to Ctrl " + key);
                    }

                Console.WriteLine("Dictionary marked aliases tested: " + recognized);
            }

            string setting = Path.Combine(Path.GetDirectoryName(typeof(RecognitionTests).Assembly.Location), "format-settings-" + Guid.NewGuid().ToString("N") + ".ini");
            File.WriteAllText(setting, "custom=keep\n");
            var preference = new FormattingSettings(setting);
            Check(preference.Italicize, "italics enabled by default");
            preference.Set(false);
            Check(!new FormattingSettings(setting).Italicize, "disabled italics saved");
            Check(File.ReadAllText(setting).Contains("custom=keep"), "unrelated setting retained");
            preference.Set(true);
            Check(new FormattingSettings(setting).Italicize, "enabled italics saved");
            File.Delete(setting);
            Console.WriteLine("PASS: " + passed + " FAIL: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
