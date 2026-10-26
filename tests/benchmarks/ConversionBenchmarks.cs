// Noninteractive conversion benchmarks. Never starts a resident, touches a live
// editor, changes the clipboard, or opens the installed user's configuration.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    public static class ConversionBenchmarks
    {
        sealed class Case
        {
            public string Id;
            public Func<string> Run;
            public Action<string> Verify;
            public Case(string id, Func<string> run, Action<string> verify = null)
            {
                Id = id;
                Run = run;
                Verify = verify;
            }
        }

        static double Percentile(double[] sorted, double fraction)
        {
            return sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * fraction) - 1)];
        }

        static Action<string> EqualTo(string expected)
        {
            return actual =>
            {
                if (actual != expected)
                    throw new InvalidOperationException("Benchmark correctness failure: " + expected);
            };
        }

        static object Measure(Case sample, int iterations, double budget)
        {
            var clock = Stopwatch.StartNew();
            string first = sample.Run();
            clock.Stop();
            double firstMs = clock.Elapsed.TotalMilliseconds;
            if (string.IsNullOrEmpty(first))
                throw new InvalidOperationException("No benchmark output: " + sample.Id);
            if (sample.Verify != null)
                sample.Verify(first);
            // JIT and first-use costs stay visible above, outside the warm series.
            for (int n = 0; n < 12; n++)
                sample.Run();
            double[] timings = new double[iterations];
            int characters = 0;
            for (int n = 0; n < timings.Length; n++)
            {
                clock.Restart();
                string value = sample.Run();
                clock.Stop();
                timings[n] = clock.Elapsed.TotalMilliseconds;
                if (string.IsNullOrEmpty(value))
                    throw new InvalidOperationException("Missing measured output: " + sample.Id);
                characters += value.Length;
            }

            Array.Sort(timings);
            double p95 = Percentile(timings, .95);
            return new
            {
                id = sample.Id,
                samples = iterations,
                first_request_ms = firstMs,
                median_ms = Percentile(timings, .5),
                p95_ms = p95,
                p99_ms = Percentile(timings, .99),
                max_ms = timings[timings.Length - 1],
                budget_ms = budget,
                within_budget = p95 <= budget,
                output_characters = characters
            };
        }

        public static int Main(string[] args)
        {
            if (args.Length != 4)
                throw new ArgumentException("Expected fixture root, output JSON, budget JSON, iterations");
            int iterations = int.Parse(args[3], CultureInfo.InvariantCulture);
            if (iterations < 20 || iterations > 10000)
                throw new ArgumentOutOfRangeException("iterations");
            var json = new JavaScriptSerializer();
            var policy = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(args[2]));
            double budget = Convert.ToDouble(policy["normal_p95_ms"], CultureInfo.InvariantCulture);
            if (double.IsNaN(budget) || double.IsInfinity(budget) || budget <= 0)
                throw new InvalidDataException("Benchmark budget must be finite and positive");
            long memoryBefore = GC.GetTotalMemory(true);
            var startup = Stopwatch.StartNew();
            using (var core = new Core(args[0]))
            {
                startup.Stop();
                double startupMs = startup.Elapsed.TotalMilliseconds;
                string paragraph = "Earlier untouched raja and Krsna. guru karma yoga śāstra śikhā mārga.";
                string large = string.Join("\n", Enumerable.Repeat(paragraph, 24));
                var cases = new[]
                {
                    new Case("word-unicode", () => core.Word("prasada", "unicode"), EqualTo("prasāda")),
                    new Case("word-balarama", () => core.Word("Krsna", "balaram"), EqualTo("Kåñëa")),
                    new Case("word-english-protection", () => core.Word("since", "unicode"), EqualTo("since")),
                    new Case("compound", () => core.Word("Vyasa-puja", "unicode"), EqualTo("Vyāsa-pūjā")),
                    new Case("cycle-choices", () => string.Join("|", core.CaretChoices("raja", "unicode"))),
                    new Case("letter-unicode", () => SingleLetters.Next("s", "unicode"), EqualTo("ṣ")),
                    new Case("letter-balarama", () => SingleLetters.Next("a", "balaram"), EqualTo("ä")),
                    new Case("short-trigger", () => core.CtrlExpansion("SP", "unicode"), EqualTo("Śrīla Prabhupāda")),
                    new Case("verse", () => core.CtrlExpansion("BG2.1", "unicode")),
                    new Case("verse-range-paired", () => core.CtrlExpansion("BG2.1-3VT", "unicode")),
                    new Case("selected-passage", () => core.ConvertHighlighted(paragraph, "unicode")),
                    new Case("selected-long-passage", () => core.ConvertHighlighted(large, "balaram")),
                    new Case("italic-decisions", () => string.Join("|", core.ItalicWords(paragraph, "unicode").Select(span => span.Start + ":" + span.Length))),
                    new Case("copy-plain", () => core.ConvertCopied("kṛṣṇa", "plain"), EqualTo("krsna")),
                    new Case("copy-itrans", () => core.ConvertCopied("kṛṣṇa", "itrans"), EqualTo("kRShNa")),
                    new Case("copy-devanagari", () => core.ConvertCopied("kṛṣṇa", "devanagari"), EqualTo("कृष्ण")),
                    new Case("copy-bengali", () => core.ConvertCopied("kṛṣṇa", "bengali"), EqualTo("কৃষ্ণ"))
                };
                var measured = cases.Select(sample => Measure(sample, iterations, budget)).ToArray();
                // Inspect the serialized shape rather than maintaining a second stats model.
                string serialized = json.Serialize(measured);
                var rows = json.Deserialize<Dictionary<string, object>[]>(serialized);
                int failures = rows.Count(row => !(bool)row["within_budget"]);
                var report = new
                {
                    schema = 1,
                    version = Version.Current,
                    utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    scope = "conversion-engine-only; no native editor, input hook or clipboard",
                    first_request_scope = "first call per case in this process; not a cold OS file-cache test",
                    clr = Environment.Version.ToString(),
                    operating_system = Environment.OSVersion.VersionString,
                    processors = Environment.ProcessorCount,
                    stopwatch_frequency = Stopwatch.Frequency,
                    is_64_bit_process = Environment.Is64BitProcess,
                    engine_initialization_ms = startupMs,
                    managed_memory_growth_bytes = GC.GetTotalMemory(false) - memoryBefore,
                    budget_failures = failures,
                    cases = measured
                };
                File.WriteAllText(args[1], json.Serialize(report), new System.Text.UTF8Encoding(false));
                Console.WriteLine("Measured " + cases.Length + " engine cases; " + iterations + " warm samples each; p95 budget failures: " + failures);
                Console.WriteLine("Engine initialization (separate): " + startupMs.ToString("F2", CultureInfo.InvariantCulture) + " ms");
                return failures == 0 ? 0 : 2;
            }
        }
    }
}
