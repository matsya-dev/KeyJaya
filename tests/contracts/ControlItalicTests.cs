// Ctrl-word/passage semantics and formatting-state cycles; no editor or input.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace KeyJaya
{
    public static class ControlItalicContracts
    {
        static int passed;
        static void Check(bool ok, string label)
        {
            if (!ok)
                throw new Exception(label);
            passed++;
        }

        static int StyleAt(ItalicSpan[] spans, int at)
        {
            var span = spans.FirstOrDefault(x => x.Start <= at && at < x.Start + x.Length);
            return span == null ? 0 : span.Italic ? 1 : -1;
        }

        public static int Run(string root)
        {
            passed = 0;
            using (var core = new Core(root, Path.Combine(root, "ctrl-italic-test-preferences.tsv")))
            {
                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "plain"
                }

                )
                {
                    foreach (string word in new[]
                    {
                        "bhakti",
                        "nitya",
                        "dvija",
                        "dvijas",
                        "dharma",
                        "dharmas",
                        "guru",
                        "gurus",
                        "yoga",
                        "yogas",
                        "karma",
                        "karmas"
                    }

                    )
                    {
                        var plan = core.PrepareControlWord(word, mode);
                        Check(plan.Text == word && plan.Choices.Length >= 2 && plan.Choices[0] == word && plan.Choices[plan.Choices.Length - 1] == word, "valid current spelling is first and original-formatting state is separate: " + word + " " + mode);
                        Check(plan.OriginalFormattingIndex == plan.Choices.Length - 1, "origin position is explicit: " + word);
                        Check(StyleAt(core.ControlItalics(null, word, mode, true), 0) == 1, "single term is italic, including English loanwords/plurals: " + word);
                        var cycle = new CaretWordCycle();
                        int[] field =
                        {
                            9,
                            3
                        };
                        cycle.Start(plan.Text, plan.Choices, mode, new IntPtr(81), field, word, plan.OriginalFormattingIndex);
                        for (int n = 1; n <= plan.Choices.Length * 2; n++)
                        {
                            string next;
                            int expected = n % plan.Choices.Length;
                            Check(cycle.Next(plan.Choices[(n - 1) % plan.Choices.Length], mode, new IntPtr(81), field, out next) && next == plan.Choices[expected], "cycle retains ordering and duplicates with different formatting");
                            Check(cycle.NextIsOriginal == (expected == plan.OriginalFormattingIndex), "only the explicit original state restores formatting");
                            cycle.Commit(next);
                        }
                    }

                    foreach (string word in new[]
                    {
                        "dharma",
                        "dharmas",
                        "guru",
                        "gurus",
                        "yoga",
                        "yogas",
                        "karma",
                        "karmas"
                    }

                    )
                        Check(StyleAt(core.ControlItalics(null, word, mode), 0) == -1, "passages explicitly make loanword exceptions regular: " + word);
                    foreach (string name in new[]
                    {
                        "Kṛṣṇa",
                        "Prabhupāda",
                        "Vṛndāvana",
                        "Gaura-pūrṇimā"
                    }

                    )
                    {
                        string encoded = Core.Target(name, mode);
                        Check(!core.ControlItalics(null, encoded, mode, true).Any(x => x.Italic), "proper name stays regular: " + name + " " + mode);
                    }

                    foreach (string compound in new[]
                    {
                        "bhakti-yoga",
                        "nitya-dharma",
                        "guru-sevā"
                    }

                    )
                    {
                        string encoded = Core.Target(compound, mode);
                        var spans = core.ControlItalics(null, encoded, mode);
                        Check(spans.Length == 1 && spans[0].Italic && spans[0].Length == encoded.Length, "Sanskrit compound is one italic term in passages: " + compound);
                    }

                    Check(core.PrepareControlWord("ordinary", mode).Choices.Length == 0 && !core.ControlItalics(null, "ordinary", mode, true).Any(x => x.Italic), "ordinary English remains protected");
                    Check(core.PrepareControlWord("sat", mode).Choices.Length == 0 && !core.ControlItalics(null, "sat", mode, true).Any(x => x.Italic), "English homograph remains protected");
                }

                string passage = "bhakti dvijas dharma gurus yoga karma Kṛṣṇa ordinary";
                var policy = core.ControlItalics(null, passage, "unicode");
                Check(StyleAt(policy, passage.IndexOf("bhakti")) == 1 && StyleAt(policy, passage.IndexOf("dvijas")) == 1, "unchanged non-diacritic terms and plurals are italic in passages");
                Check(StyleAt(policy, passage.IndexOf("dharma")) == -1 && StyleAt(policy, passage.IndexOf("gurus")) == -1, "explicit regular spans work even without text replacement");
                Check(StyleAt(policy, passage.IndexOf("ordinary")) == 0, "unrelated English formatting is not normalized");
                Check(core.ControlItalics(null, "Śrī Bhagavad-gītā", "unicode").Where(x => x.Italic).Sum(x => x.Length) == "Śrī Bhagavad-gītā".Length, "book-title prefix keeps its established italics rather than being mistaken for a personal name");
                Check(core.ControlItalics(null, "Śrīmad-Bhāgavatam", "unicode", true).Any(x => x.Italic), "individual book titles retain established style");
                Check(Core.SingleControlWord(" (guru), ") && !Core.SingleControlWord("guru and bhakti") && !Core.SingleControlWord("guru 24") && !Core.SingleControlWord("`guru`"), "single-word selection detection protects passages, numbers and code");
                var punctuated = core.PrepareControlWord(" (dharma), ", "unicode");
                Check(punctuated.Text == " (dharma), " && punctuated.OriginalFormattingIndex == 1, "single selected word keeps whitespace and punctuation");
                Check(core.ControlItalics(null, "dharma", "devanagari", true).Length == 0 && core.ControlItalics(null, "dharma", "bengali", true).Length == 0 && core.ControlItalics(null, "dharma", "itrans", true).Length == 0, "script outputs retain their no-italics rule");
                string html = RichClipboard.Fragment("bhakti dharma", core.ControlItalics(null, "bhakti dharma", "unicode"));
                Check(html.Contains("<i>bhakti</i>") && html.Contains("font-style:normal\">dharma</span>"), "rich HTML explicitly carries both italic and regular style");
                Func<string, ItalicSpan[]> passagePolicy = text => core.ControlItalics(null, text, "unicode");
                Check(MarkdownFormatting.Apply("*dharma* and bhakti", passagePolicy) == "dharma and *bhakti*", "Markdown removes existing exception italics and adds unchanged-word italics");
                Check(MarkdownFormatting.Apply("***dharma*** and **bhakti**", passagePolicy) == "**dharma** and ***bhakti***", "Markdown preserves bold while changing italics");
                Check(MarkdownFormatting.Apply("`*dharma*` [bhakti](https://example.test)", passagePolicy) == "`*dharma*` [bhakti](https://example.test)", "Markdown code and links remain unchanged");
                Check(MarkdownFormatting.Apply("*bhakti and dharma*", passagePolicy) == "*bhakti and* dharma", "Markdown preserves unrelated inherited italics while making the exception regular");
                var merged = WordSelection.MergeVerseSpans("bhakti dharma", new[] { new ItalicSpan(0, 6, true), new ItalicSpan(7, 6, true, false) });
                Check(merged.Length == 2 && merged[0].Italic && !merged[1].Italic, "Word verse merging never swallows a regular-type span");
                var original = core.PrepareWordConversion("bhakti", "unicode");
                Check(original.OriginalFormattingIndex == -1, "ordinary engine conversion keeps its existing cycle semantics");
            }

            Console.WriteLine("Ctrl italics: " + passed + " assertions; plain spellings, plurals, loanwords, names, compounds, explicit roman spans and formatting cycles.");
            return passed;
        }
    }
}
