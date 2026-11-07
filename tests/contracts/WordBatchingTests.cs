// Production patch loop against counted in-memory ranges. Never starts Word.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace KeyJaya
{
    internal static class WordBatchingTests
    {
        const string Header = "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p>";
        const string Footer = "</w:p></w:body></w:document>";
        static string Xml(string text, string properties = "")
        {
            return Header + "<w:r>" + properties + "<w:t>" + SecurityElement.Escape(text) + "</w:t></w:r>" + Footer;
        }

        sealed class Model
        {
            public string Text = "krsna";
            public int[] Fonts =
            {
                1,
                1,
                1,
                1,
                1
            };
            public int Writes, Scans, Exports;
            public string Properties = "";
            public bool Mixed, NamedStyle, MissingMetadata, BrokenXml, ThrowXml, FailScan, SubstituteFont, Current = true;
            public Action Exported, Written;
        }

        sealed class Font
        {
            public int Token;
            public string Name { get; set; }

            public Font Duplicate
            {
                get
                {
                    return new Font
                    {
                        Token = Token,
                        Name = Name
                    };
                }
            }
        }

        sealed class Style
        {
            public int Type { get; set; }
        }

        sealed class Revisions
        {
            readonly Model model;
            public Revisions(Model value)
            {
                model = value;
            }

            public int Count
            {
                get
                {
                    model.Scans++;
                    if (model.FailScan)
                        throw new InvalidOperationException("Revision provider unavailable");
                    return 0;
                }
            }
        }

        sealed class Range
        {
            readonly Model model;
            int begin, end;
            readonly bool story;
            public Range(Model value, int begin, int end, bool story = false)
            {
                model = value;
                this.begin = begin;
                this.end = end;
                this.story = story;
            }

            public int Start
            {
                get
                {
                    return begin;
                }
            }

            public int End
            {
                get
                {
                    return story ? model.Text.Length : end;
                }
            }

            public Range Duplicate
            {
                get
                {
                    return new Range(model, begin, End);
                }
            }

            public void SetRange(int first, int last)
            {
                begin = first;
                end = last;
            }

            public Revisions Revisions
            {
                get
                {
                    return new Revisions(model);
                }
            }

            public Font Font
            {
                get
                {
                    return new Font
                    {
                        Token = model.Fonts[begin],
                        Name = model.Fonts.Skip(begin).Take(End - begin).Distinct().Count() == 1 ? "Model" : ""
                    };
                }

                set
                {
                    for (int n = begin; n < End; n++)
                        model.Fonts[n] = value.Token;
                }
            }

            public Style Style
            {
                get
                {
                    if (model.MissingMetadata)
                        throw new InvalidOperationException("Optional formatting metadata unavailable");
                    return new Style
                    {
                        Type = model.NamedStyle ? 2 : 1
                    };
                }
            }

            public string Text
            {
                get
                {
                    return model.Text.Substring(begin, End - begin);
                }

                set
                {
                    int original = model.SubstituteFont && value.Any(letter => letter > 127) ? 99 : model.Fonts[begin];
                    model.Fonts = model.Fonts.Take(begin).Concat(Enumerable.Repeat(original, value.Length)).Concat(model.Fonts.Skip(End)).ToArray();
                    model.Text = model.Text.Remove(begin, End - begin).Insert(begin, value);
                    end = begin + value.Length;
                    model.Writes++;
                    if (model.Written != null)
                        model.Written();
                }
            }

            public string WordOpenXML
            {
                get
                {
                    model.Exports++;
                    if (model.ThrowXml)
                        throw new InvalidOperationException("Optional export unavailable");
                    string text = Text;
                    string xml = model.BrokenXml ? "<broken>" : model.Mixed ? Header + string.Concat(text.Select(letter => "<w:r><w:t>" + letter + "</w:t></w:r>")) + Footer : Xml(text, model.Properties);
                    if (model.Exported != null)
                        model.Exported();
                    return xml;
                }
            }
        }

        static WordSelection.VisibleMap Edit(Model model, bool batch, out bool changed, out string current)
        {
            var map = WordSelection.VisibleMap.Create(model.Text, 0, model.Text.Length, new int[0], new int[0]);
            var patches = SelectionFormatting.Plan(model.Text, "kṛṣṇa");
            int end = model.Text.Length, storyEnd = end;
            changed = false;
            current = model.Text;
            patches = WordSelection.PreparePatches(new Range(model, 0, end), map, patches, () => model.Current, batch);
            WordSelection.ApplyPatches(new Range(model, 0, end), new Range(model, 0, end, true), ref map, patches, () => model.Current, ref changed, ref current, ref end, ref storyEnd);
            return map;
        }

        internal static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (value, message) =>
            {
                if (!value)
                    throw new Exception("Word batching: " + message);
                passed++;
            };
            check(WordSelection.PlainRun(Xml("rsn"), "rsn"), "one exact plain run qualifies");
            string defaultStyle = "<root xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:styles><w:style w:styleId='DefaultParagraphFont' w:type='character' w:default='1'/></w:styles>" + Xml("rsn", "<w:rPr><w:rStyle w:val='DefaultParagraphFont'/></w:rPr>") + "</root>";
            check(WordSelection.PlainRun(defaultStyle, "rsn"), "export's explicitly defined unmodified default character style remains plain");
            check(WordSelection.PlainRun(defaultStyle.Replace("DefaultParagraphFont", "LocalizedDefault"), "rsn"), "default definition is proved without relying on a localized style ID");
            check(!WordSelection.PlainRun(defaultStyle.Replace("w:default='1'", "w:default='0'"), "rsn"), "a named style is not a default-style proof");
            check(!WordSelection.PlainRun(defaultStyle.Replace("w:default='1'/>", "w:default='1'><w:rPr><w:b/></w:rPr></w:style>"), "rsn"), "customized default character style retains old edits");
            check(WordSelection.PlainRun(Xml("rsn", "<w:rPr><w:rFonts w:ascii='Calibri'/><w:sz w:val='24'/><w:lang w:val='en-US'/></w:rPr>"), "rsn"), "uniform ordinary font properties qualify");
            check(!WordSelection.PlainRun(Xml("rsn"), "changed"), "text mismatch never authorizes a group");
            foreach (string markup in new[]
            {
                "<w:bookmarkStart w:id='1' w:name='mark'/>",
                "<w:commentRangeStart w:id='1'/>",
                "<w:commentReference w:id='1'/>",
                "<w:fldChar w:fldCharType='begin'/>",
                "<w:ins><w:r><w:t>rsn</w:t></w:r></w:ins>",
                "<w:del><w:r><w:delText>rsn</w:delText></w:r></w:del>",
                "<w:hyperlink><w:r><w:t>rsn</w:t></w:r></w:hyperlink>"
            }

            )
                check(!WordSelection.PlainRun(Header + markup + "<w:r><w:t>rsn</w:t></w:r>" + Footer, "rsn"), "anchors, fields, links and revisions retain letter edits");
            foreach (string property in new[]
            {
                "rStyle",
                "rPrChange",
                "highlight",
                "vanish",
                "unknown"
            }

            )
                check(!WordSelection.PlainRun(Xml("rsn", "<w:rPr><w:" + property + " w:val='1'/></w:rPr>"), "rsn"), "unsupported/semantic run properties retain letter edits");
            check(!WordSelection.PlainRun(Header + "<w:r><w:t>r</w:t></w:r><w:r><w:t>sn</w:t></w:r>" + Footer, "rsn"), "multiple formatting runs are not uniform proof");
            check(!WordSelection.PlainRun("<!DOCTYPE x [<!ENTITY a 'rsn'>]>" + Xml("rsn"), "rsn"), "DTD cannot supply formatting evidence");
            check(!WordSelection.PlainRun("<broken>", "rsn") && !WordSelection.PlainRun(new string ('x', 2097153), "rsn"), "unavailable/oversized export falls back");
            var patches = SelectionFormatting.Plan("krsna", "kṛṣṇa");
            check(WordSelection.AdjacentPatches(patches, 0) == 3, "only the three adjacent changed letters form a candidate");
            check(WordSelection.AdjacentPatches(new[] { new TextPatch(0, "a", "ā\u0301") }, 0) == 1, "combining/length-changing patches retain their original path");
            check(WordSelection.AdjacentPatches(new[] { new TextPatch(1, "a", "ā"), new TextPatch(0, "क", "ख") }, 0) == 1, "non-Latin scripts retain their established edits");
            var many = Enumerable.Range(0, 33).Select(n => new TextPatch(32 - n, "r", "ṛ")).ToArray();
            check(WordSelection.AdjacentPatches(many, 0) == 32, "group length is bounded");
            bool changed;
            string current;
            var plain = new Model();
            var result = Edit(plain, true, out changed, out current);
            check(changed && result.Value == "kṛṣṇa" && current == "kṛṣṇa" && plain.Text == "kṛṣṇa", "production grouped edit produces the exact result");
            check(plain.Writes == 1 && plain.Scans == 1 && plain.Exports == 1 && plain.Fonts.All(value => value == 1), "one text edit/revision scan replaces three while preserving font tokens");
            var previous = new Model();
            Edit(previous, false, out changed, out current);
            check(previous.Text == plain.Text && previous.Writes == 3 && previous.Scans == 3 && previous.Exports == 0, "existing path remains available without extra provider work");
            var substituted = new Model
            {
                SubstituteFont = true
            };
            Edit(substituted, false, out changed, out current);
            check(substituted.Text == "kṛṣṇa" && substituted.Writes == 3 && substituted.Fonts.All(value => value == 1), "one-letter edits restore their source font when Word substitutes a diacritic font");
            var deleted = new Model
            {
                Text = "krXsna",
                Fonts = new[]
                {
                    1,
                    1,
                    1,
                    1,
                    1,
                    1
                }
            };
            var deletionMap = WordSelection.VisibleMap.Create(deleted.Text, 0, 6, new[] { 2 }, new[] { 3 });
            int deletionEnd = 6, deletionStoryEnd = 6;
            changed = false;
            current = "krsna";
            WordSelection.ApplyPatches(new Range(deleted, 0, 6), new Range(deleted, 0, 6, true), ref deletionMap, WordSelection.PreparePatches(new Range(deleted, 0, 6), deletionMap, patches, () => true, true), () => true, ref changed, ref current, ref deletionEnd, ref deletionStoryEnd);
            check(deleted.Text == "kṛXṣṇa" && deletionMap.Value == "kṛṣṇa" && deleted.Writes == 3 && deleted.Scans == 3 && deleted.Exports == 0, "existing deleted native positions prevent batching without another export");
            var mixed = new Model
            {
                Mixed = true,
                Fonts = new[]
                {
                    1,
                    2,
                    1,
                    1,
                    1
                }
            };
            Edit(mixed, true, out changed, out current);
            check(mixed.Writes == 3 && mixed.Scans == 3 && mixed.Exports == 0 && mixed.Fonts.SequenceEqual(new[] { 1, 2, 1, 1, 1 }), "mixed font rejects the expensive export and preserves every character's font");
            var named = new Model
            {
                NamedStyle = true
            };
            Edit(named, true, out changed, out current);
            check(named.Writes == 3 && named.Scans == 3 && named.Exports == 0 && named.Fonts.All(value => value == 1), "named character style keeps letter edits without an XML export");
            var missing = new Model
            {
                MissingMetadata = true
            };
            Edit(missing, true, out changed, out current);
            check(missing.Writes == 1 && missing.Exports == 1, "missing rejection metadata still requires successful XML proof");
            missing = new Model
            {
                MissingMetadata = true,
                BrokenXml = true
            };
            Edit(missing, true, out changed, out current);
            check(missing.Writes == 3 && missing.Exports == 1, "missing metadata and failed XML never authorize a group");
            foreach (var fallback in new[]
            {
                new Model
                {
                    ThrowXml = true
                },
                new Model
                {
                    BrokenXml = true
                },
                new Model
                {
                    Properties = "<w:rPr><w:rStyle w:val='Emphasis'/></w:rPr>"
                }
            }

            )
            {
                Edit(fallback, true, out changed, out current);
                check(fallback.Text == "kṛṣṇa" && fallback.Writes == 3 && fallback.Scans == 3 && fallback.Exports == 1, "unavailable XML/character style keeps the exact old editing behavior");
            }

            var cancelled = new Model();
            cancelled.Exported = () => cancelled.Current = false;
            bool refused = false;
            try
            {
                Edit(cancelled, true, out changed, out current);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            check(refused && cancelled.Writes == 0, "cancellation during export cannot authorize an edit");
            var stale = new Model
            {
                BrokenXml = true
            };
            stale.Exported = () => stale.Text = "other";
            refused = false;
            try
            {
                Edit(stale, true, out changed, out current);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            check(refused && stale.Writes == 0, "source changed during a declined export is revalidated before fallback");
            var interrupted = new Model
            {
                Mixed = true
            };
            interrupted.Written = () => interrupted.Current = false;
            var partial = WordSelection.VisibleMap.Create("krsna", 0, 5, new int[0], new int[0]);
            int partialEnd = 5, partialStoryEnd = 5;
            changed = false;
            current = "krsna";
            refused = false;
            try
            {
                WordSelection.ApplyPatches(new Range(interrupted, 0, 5), new Range(interrupted, 0, 5, true), ref partial, WordSelection.PreparePatches(new Range(interrupted, 0, 5), partial, patches, () => interrupted.Current, true), () => interrupted.Current, ref changed, ref current, ref partialEnd, ref partialStoryEnd);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            check(refused && changed && interrupted.Writes == 1 && current == "krsṇa" && partial.Value == current && partial.MatchesRaw(interrupted.Text), "partial progress remains published for the existing owned rollback proof");
            var failed = new Model
            {
                FailScan = true
            };
            refused = false;
            try
            {
                Edit(failed, true, out changed, out current);
            }
            catch (Exception)
            {
                refused = true;
            }

            check(refused && failed.Writes == 1, "inserted revision failures still abort the owned transaction");
            Console.WriteLine("Word batching: " + passed + " assertions; real patch loop, counted edits/scans, mixed-format fallback, cancellation and revision failures; no Word access.");
            return passed;
        }
    }
}
