// Pure fixture/report contracts. No Word process or desktop access.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    internal static class WordPerformanceContracts
    {
        internal static int Run(string fixturesJson)
        {
            int checks = 0;
            Action<bool, string> check = (value, message) =>
            {
                if (!value)
                    throw new Exception("Word performance contracts: " + message);
                checks++;
            };
            Action<Action, string> rejects = (action, message) =>
            {
                bool refused = false;
                try
                {
                    action();
                }
                catch (Exception)
                {
                    refused = true;
                }

                check(refused, message);
            };
            var plan = WordPerformanceSupport.Parse(fixturesJson);
            check(plan.cases.Length == 9 && plan.cases.Any(item => item.character_style) && plan.cases.Any(item => item.id == "mixed-word" && item.mixed), "fixed matrix retains original workloads and both formatting fallbacks");
            check(plan.cases.Any(item => item.kind == "caret") && plan.cases.Any(item => item.id == "selected-word") && plan.cases.Any(item => item.id == "selected-passage"), "caret, word and passage are distinct scenarios");
            check(plan.cases.Any(item => item.mixed) && plan.cases.Any(item => item.deletion >= 0) && plan.cases.Any(item => item.input == item.output) && plan.cases.Any(item => item.input.Length != item.output.Length), "mixed formatting, existing deletions, style-only and changed lengths are represented");
            var trials = WordPerformanceSupport.Schedule(plan, 5, 1);
            check(WordPerformanceSupport.Filter(plan, "all") == plan, "unfiltered run keeps the complete fixture matrix");
            check(WordPerformanceSupport.Schedule(WordPerformanceSupport.Filter(plan, "mixed-format"), 3, 1).Count == 16, "focused debugging retains both variants and tracking states");
            rejects(() => WordPerformanceSupport.Filter(plan, "missing"), "unknown requested fixture is refused");
            rejects(() => WordPerformanceSupport.Filter(plan, "mixed-format,mixed-format"), "duplicate requested fixture is refused");
            check(trials.Count == 9 * 2 * 2 * 6, "both variants and Track Changes states receive identical repetitions");
            for (int n = 0; n < trials.Count; n += 2)
            {
                check(trials[n].fixture == trials[n + 1].fixture && trials[n].tracking == trials[n + 1].tracking && trials[n].iteration == trials[n + 1].iteration && trials[n].variant != trials[n + 1].variant, "comparison pairs share workload, tracking and iteration");
            }

            foreach (var group in trials.GroupBy(trial => trial.fixture.id + "/" + trial.tracking))
                check(group.Where((trial, n) => n % 2 == 0).Count(trial => trial.variant == "baseline") == 3, "baseline/candidate order is balanced across repetitions");
            var rows = trials.Select(trial => new WordPerformanceSample { id = trial.fixture.id, variant = trial.variant, tracking = trial.tracking, iteration = trial.iteration, warmup = trial.warmup, status = "passed", checks = 1, elapsed_ms = trial.warmup ? 1000 : trial.variant == "baseline" ? 20 : 10 }).ToList();
            WordPerformanceSupport.ValidateSamples(trials, rows);
            var json = new JavaScriptSerializer();
            string summary = json.Serialize(WordPerformanceSupport.Summarize(trials, rows, true));
            check(summary.Contains("calibration; no improvement claim") && summary.Contains("baseline_median_ms\":20") && summary.Contains("candidate_median_ms\":10"), "warmups are excluded and identical-source noise cannot be an improvement claim");
            summary = json.Serialize(WordPerformanceSupport.Summarize(trials, rows, false));
            check(summary.Contains("median_improvement_percent\":50"), "different-source median comparison retains the measured ratio");
            check(WordPerformanceSupport.Median(new[] { 1.0, 2, 3, 4 }) == 2.5 && WordPerformanceSupport.Percentile(new[] { 9.0, 1, 4, 2 }, .95) == 9, "median and nearest-rank p95 have independent expected values");
            rejects(() => WordPerformanceSupport.ValidateSamples(trials, rows.Take(rows.Count - 1).ToList()), "missing sample refuses a comparison");
            var duplicate = rows.ToList();
            duplicate[1] = duplicate[0];
            rejects(() => WordPerformanceSupport.ValidateSamples(trials, duplicate), "duplicate samples cannot replace missing work");
            rows[0].status = "failed";
            rejects(() => WordPerformanceSupport.Summarize(trials, rows, false), "failed warmup cannot be discarded to manufacture success");
            rows[0].status = "passed";
            rows[0].elapsed_ms = double.NaN;
            rejects(() => WordPerformanceSupport.ValidateSamples(trials, rows), "NaN timings are rejected");
            rows[0].elapsed_ms = -1;
            rejects(() => WordPerformanceSupport.ValidateSamples(trials, rows), "negative timings are rejected");
            rejects(() => WordPerformanceSupport.Schedule(plan, 2, 1), "insufficient repetitions are refused");
            rejects(() => WordPerformanceSupport.Schedule(plan, 5, 0), "warmup cannot silently be omitted");
            rejects(() => WordPerformanceSupport.Parse(fixturesJson.Replace("\"schema\": 1", "\"schema\": 9")), "unknown fixture schema is refused");
            rejects(() => WordPerformanceSupport.Parse(fixturesJson.Replace("\"id\":\"selected-word\"", "\"id\":\"caret-word\"")), "duplicate fixture IDs are refused");
            rejects(() => WordPerformanceSupport.Parse(fixturesJson.Replace("\"deletion\":2", "\"deletion\":3")), "wrong tracked-deletion seed is refused");
            long[] activation =
            {
                0,
                0,
                7
            };
            int read = 0, pauses = 0;
            check(WordPerformanceSupport.WaitForOwnedForeground(7, () => activation[Math.Min(read++, 2)], () => true, () => pauses++) && pauses == 2, "verification can wait for a null foreground transition without reclaiming focus");
            long[] foreign =
            {
                0,
                9,
                7
            };
            read = pauses = 0;
            check(!WordPerformanceSupport.WaitForOwnedForeground(7, () => foreign[Math.Min(read++, 2)], () => true, () => pauses++) && pauses == 1, "any different foreground window aborts immediately");
            pauses = 0;
            check(!WordPerformanceSupport.WaitForOwnedForeground(7, () => 0, () => true, () => pauses++) && pauses == 5, "missing foreground remains a bounded refusal");
            check(!WordPerformanceSupport.WaitForOwnedForeground(7, () => 7, () => false, () =>
            {
                throw new Exception("Unexpected pause");
            }), "the original native focus identity is still required before undo");
            const string start = "<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body><w:p>";
            const string end = "</w:p></w:body></w:document>";
            string xml = start + "<w:r><w:t>k</w:t></w:r><w:del><w:r><w:delText>rsn</w:delText></w:r></w:del><w:ins><w:r><w:t>ṛṣṇ</w:t></w:r></w:ins><w:r><w:t>a</w:t></w:r>" + end;
            check(WordPerformanceSupport.VisibleBody(xml) == "kṛṣṇa", "independent XML oracle includes insertions and excludes deleted text");
            xml = start + "<w:r><w:t>a</w:t></w:r><w:moveFrom><w:r><w:t>hidden</w:t></w:r></w:moveFrom><w:r><w:tab/><w:t>b</w:t><w:br/></w:r>" + end;
            check(WordPerformanceSupport.VisibleBody(xml) == "a\tb\n", "XML oracle excludes move-from content and retains tabs/breaks");
            rejects(() => WordPerformanceSupport.VisibleBody(start + end.Replace("</w:body>", "<w:p/></w:body>")), "unexpected extra paragraphs cannot be silently omitted");
            rejects(() => WordPerformanceSupport.VisibleBody("<!DOCTYPE x [<!ENTITY content 'private'>]>" + start + "<w:r><w:t>&content;</w:t></w:r>" + end), "XML entities are disabled");
            Console.WriteLine("Word performance automation: " + checks + " assertions; complete paired matrix, warmups, failed/duplicate samples, timing statistics and independent XML oracle; no Word access.");
            return checks;
        }
    }
}
