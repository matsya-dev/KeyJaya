// Test-only schedule, report validation and independent scratch-document oracle.
// No UIA, Office activation, hooks, clipboard or installed profile access.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;

namespace KeyJaya
{
    public sealed class WordPerformancePlan
    {
        public int schema;
        public WordPerformanceCase[] cases;
    }

    public sealed class WordPerformanceCase
    {
        public string id, kind, input, output, seed;
        public bool mixed, character_style;
        public int deletion = -1;
        public WordPerformanceSpan[] italics;
    }

    public sealed class WordPerformanceSpan
    {
        public int start, length;
        public bool italic;
    }

    public sealed class WordPerformanceTrial
    {
        public WordPerformanceCase fixture;
        public string variant;
        public bool tracking, warmup;
        public int iteration;
        public string Key()
        {
            return fixture.id + "/" + tracking + "/" + variant + "/" + iteration;
        }
    }

    public sealed class WordPerformanceSample
    {
        public string id, variant, status;
        public bool tracking, warmup;
        public int iteration, checks;
        public double elapsed_ms;
        public string[] native_trace;
        public string Key()
        {
            return id + "/" + tracking + "/" + variant + "/" + iteration;
        }
    }

    internal static class WordPerformanceSupport
    {
        internal static void Require(bool value, string message)
        {
            if (!value)
                throw new InvalidOperationException(message);
        }

        // Verification only: Windows may briefly return no foreground window
        // during activation. Never wait through a different foreground owner,
        // reclaim focus or permit undo without the original native focus proof.
        internal static bool WaitForOwnedForeground(long owner, Func<long> foreground, Func<bool> context, Action pause)
        {
            for (int poll = 0; poll < 5; poll++)
            {
                long current = foreground();
                if (current == owner)
                    return context();
                if (current != 0)
                    return false;
                pause();
            }

            return false;
        }

        internal static WordPerformancePlan Parse(string json)
        {
            var plan = new JavaScriptSerializer().Deserialize<WordPerformancePlan>(json);
            Require(plan != null && plan.schema == 1 && plan.cases != null && plan.cases.Length > 0 && plan.cases.Length <= 32, "Invalid Word performance fixture schema/count");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in plan.cases)
            {
                Require(item != null && !string.IsNullOrEmpty(item.id) && System.Text.RegularExpressions.Regex.IsMatch(item.id, "^[a-z0-9-]{1,48}$") && ids.Add(item.id), "Invalid/duplicate fixture ID");
                Require(item.kind == "caret" || item.kind == "selection", "Unknown fixture selection mode");
                Require(!string.IsNullOrEmpty(item.input) && !string.IsNullOrEmpty(item.output) && item.input.Length <= 256 && item.output.Length <= 256 && !item.input.Any(char.IsControl) && !item.output.Any(char.IsControl), "Fixture requires bounded single-paragraph text");
                Require(!item.mixed || item.input.Length == item.output.Length, "Mixed-font oracle requires aligned fixture text");
                Require(item.kind != "caret" || item.input.All(char.IsLetter), "Caret fixture must be one word");
                if (item.deletion >= 0)
                    Require(item.seed != null && item.deletion < item.seed.Length && item.seed.Remove(item.deletion, 1) == item.input, "Tracked-deletion seed must produce the exact visible input");
                else
                    Require(item.seed == null, "Seed requires an explicit deletion");
                Require(item.italics != null && item.italics.Length > 0, "Fixture needs explicit formatting expectations");
                foreach (var span in item.italics)
                    Require(span != null && span.start >= 0 && span.length > 0 && span.start <= item.output.Length - span.length, "Invalid fixture formatting span");
            }

            return plan;
        }

        internal static WordPerformancePlan Filter(WordPerformancePlan plan, string selection)
        {
            if (selection == "all")
                return plan;
            var ids = selection.Split(',');
            Require(ids.Length > 0 && ids.Distinct(StringComparer.Ordinal).Count() == ids.Length && ids.All(id => plan.cases.Any(item => item.id == id)), "Unknown/duplicate selected fixture");
            return new WordPerformancePlan
            {
                schema = plan.schema,
                cases = plan.cases.Where(item => ids.Contains(item.id)).ToArray()
            };
        }

        internal static List<WordPerformanceTrial> Schedule(WordPerformancePlan plan, int iterations, int warmups)
        {
            Require(iterations >= 3 && iterations <= 31 && warmups >= 1 && warmups <= 3, "Bounded repetitions required");
            var trials = new List<WordPerformanceTrial>();
            foreach (var fixture in plan.cases)
                foreach (bool tracking in new[]
                {
                    false,
                    true
                }

                )
                    for (int iteration = -warmups; iteration < iterations; iteration++)
                        foreach (string variant in iteration % 2 == 0 ? new[]
                        {
                            "baseline",
                            "candidate"
                        }

                        : new[]
                        {
                            "candidate",
                            "baseline"
                        }

                        )
                            trials.Add(new WordPerformanceTrial { fixture = fixture, tracking = tracking, variant = variant, iteration = iteration, warmup = iteration < 0 });
            return trials;
        }

        internal static void ValidateSamples(IList<WordPerformanceTrial> trials, IList<WordPerformanceSample> samples)
        {
            Require(samples.Count == trials.Count, "Incomplete Word comparison: missing samples");
            var expected = trials.ToDictionary(item => item.Key(), StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sample in samples)
            {
                WordPerformanceTrial trial;
                Require(sample != null && seen.Add(sample.Key()) && expected.TryGetValue(sample.Key(), out trial), "Unexpected or duplicate comparison sample");
                trial = expected[sample.Key()];
                Require(sample.status == "passed" && sample.checks > 0 && sample.warmup == trial.warmup && !double.IsNaN(sample.elapsed_ms) && !double.IsInfinity(sample.elapsed_ms) && sample.elapsed_ms >= 0, "Failed/invalid samples cannot authorize a performance result");
            }
        }

        internal static double Percentile(IEnumerable<double> values, double percentile)
        {
            double[] ordered = values.OrderBy(value => value).ToArray();
            Require(ordered.Length > 0 && ordered.All(value => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0) && percentile > 0 && percentile <= 1, "Invalid timing population");
            return ordered[Math.Max(0, (int)Math.Ceiling(ordered.Length * percentile) - 1)];
        }

        internal static double Median(IEnumerable<double> values)
        {
            double[] ordered = values.OrderBy(value => value).ToArray();
            Percentile(ordered, .5);
            return ordered.Length % 2 == 1 ? ordered[ordered.Length / 2] : (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2;
        }

        internal static object Summarize(IList<WordPerformanceTrial> trials, IList<WordPerformanceSample> samples, bool sameSource)
        {
            ValidateSamples(trials, samples);
            var result = new List<object>();
            foreach (var group in samples.GroupBy(sample => sample.id + "/" + sample.tracking))
            {
                var baseline = group.Where(sample => sample.variant == "baseline" && !sample.warmup).Select(sample => sample.elapsed_ms).ToArray();
                var candidate = group.Where(sample => sample.variant == "candidate" && !sample.warmup).Select(sample => sample.elapsed_ms).ToArray();
                double before = Median(baseline), after = Median(candidate);
                result.Add(new { id = group.First().id, tracking = group.First().tracking, samples_per_variant = baseline.Length, baseline_first_ms = group.First(sample => sample.variant == "baseline").elapsed_ms, candidate_first_ms = group.First(sample => sample.variant == "candidate").elapsed_ms, baseline_median_ms = before, candidate_median_ms = after, baseline_p95_ms = Percentile(baseline, .95), candidate_p95_ms = Percentile(candidate, .95), median_improvement_percent = before == 0 ? (double? )null : 100 * (before - after) / before, comparison = sameSource ? "same-source calibration; no improvement claim" : "measured comparison; review every scenario" });
            }

            return result;
        }

        internal static string VisibleBody(string xml)
        {
            var doc = new XmlDocument
            {
                XmlResolver = null
            };
            using (var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                doc.Load(reader);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
            var paragraphs = doc.SelectNodes("//w:document/w:body/w:p", ns);
            Require(paragraphs.Count == 1, "Scratch oracle requires exactly one body paragraph");
            var text = new StringBuilder();
            foreach (XmlNode node in paragraphs[0].SelectNodes(".//w:t | .//w:tab | .//w:br", ns))
            {
                bool hidden = false;
                for (var parent = node.ParentNode; parent != null; parent = parent.ParentNode)
                    hidden |= parent.NamespaceURI == ns.LookupNamespace("w") && (parent.LocalName == "del" || parent.LocalName == "moveFrom");
                if (!hidden)
                    text.Append(node.LocalName == "t" ? node.InnerText : node.LocalName == "tab" ? "\t" : "\n");
            }

            return text.ToString();
        }
    }
}
