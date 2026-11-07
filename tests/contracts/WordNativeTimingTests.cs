// Production reflection calls with an in-memory provider. No Word, UIA, input,
// clipboard or documents; verify timing observes without changing or leaking data.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace KeyJaya
{
    internal static class WordNativeTimingTests
    {
        sealed class Provider
        {
            public string Text { get; set; }

            public void Fail()
            {
                throw new InvalidOperationException("private failure detail");
            }
        }

        static object Invoke(Provider value, string member, BindingFlags kind, object[] arguments)
        {
            return typeof(WordSelection).GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { value, member, kind, arguments });
        }

        internal static int Run()
        {
            int passed = 0;
            Action<bool, string> check = (valid, message) =>
            {
                if (!valid)
                    throw new Exception("Word timing: " + message);
                passed++;
            };
            var records = new List<string>();
            var provider = new Provider
            {
                Text = "private comment text"
            };
            using (WordNativeTiming.Begin(records.Add))
            {
                check(WordNativeTiming.Active, "diagnostic scope is active only on its calling thread");
                check((string)Invoke(provider, "Text", BindingFlags.GetProperty, null) == "private comment text", "observation preserves the provider result");
                WordNativeTiming.Next("patches");
                Invoke(provider, "Text", BindingFlags.SetProperty, new object[] { "private replacement" });
                WordNativeTiming.Committed();
            }

            check(!WordNativeTiming.Active && provider.Text == "private replacement", "scope closes without changing the applied value");
            check(records.Count == 1 && records[0].Contains("committed=True") && records[0].Contains("[focus ms=") && records[0].Contains("[patches ms="), "one record retains stage and result metadata");
            check(records[0].Contains("calls=1 gets=1 sets=0") && records[0].Contains("calls=1 gets=0 sets=1"), "native property reads and writes are counted per phase");
            check(!records[0].Contains("private") && !records[0].Contains("replacement"), "field values never enter timing output");
            bool failed = false;
            using (WordNativeTiming.Begin(records.Add))
                try
                {
                    Invoke(provider, "Fail", BindingFlags.InvokeMethod, null);
                }
                catch (TargetInvocationException error)
                {
                    failed = error.InnerException is TargetInvocationException && error.InnerException.InnerException is InvalidOperationException;
                }

            check(failed && records[1].Contains("committed=False") && records[1].Contains("calls=1"), "failed calls preserve their exception and remain uncommitted");
            check(!records[1].Contains("private failure"), "provider exception details are not logged by timing");
            using (WordNativeTiming.Begin(message =>
            {
                throw new Exception("observer unavailable");
            }))
                check((string)Invoke(provider, "Text", BindingFlags.GetProperty, null) == "private replacement", "failed observer cannot affect a conversion result");
            check(!WordNativeTiming.Active && WordNativeTiming.Begin(null) == null, "diagnostics disabled leaves no active scope");
            records.Clear();
            using (WordNativeTiming.Begin(records.Add))
            {
                Invoke(provider, "Text", BindingFlags.GetProperty, null);
                using (WordNativeTiming.Begin(records.Add))
                    Invoke(provider, "Text", BindingFlags.GetProperty, null);
                Invoke(provider, "Text", BindingFlags.GetProperty, null);
            }

            check(records.Count == 2 && records[0].Contains("calls=1") && records[1].Contains("calls=2"), "nested observers restore their caller's measurements");
            records.Clear();
            int releases = 0;
            WordNativeTiming.Release(() => releases++);
            check(releases == 1 && records.Count == 0, "disabled diagnostics execute cleanup once without a record");
            using (WordNativeTiming.Begin(records.Add))
            {
                WordNativeTiming.Release(() => releases++);
                WordNativeTiming.Next("cleanup");
                WordNativeTiming.Release(() => releases++);
            }

            check(releases == 3 && records.Count == 1 && records[0].Contains("releases=1 release_ms=") && records[0].Contains("release_max_ms="), "release work is counted separately by phase");
            failed = false;
            using (WordNativeTiming.Begin(records.Add))
                try
                {
                    WordNativeTiming.Release(() =>
                    {
                        throw new InvalidOperationException("private release detail");
                    });
                }
                catch (InvalidOperationException)
                {
                    failed = true;
                }

            check(failed && records[1].Contains("releases=1"), "failed release retains its exception and timing count");
            check(!records[1].Contains("private"), "release diagnostics expose no provider messages or values");
            records.Clear();
            check(WordNativeTiming.Measure("Revision.Next", () => 1) == 1 && records.Count == 0, "disabled enumeration diagnostics retain status without a record");
            var enumerationError = new InvalidOperationException("private enumeration detail");
            Exception observedError = null;
            using (WordNativeTiming.Begin(records.Add))
            {
                check(WordNativeTiming.Measure("Revision.Next", () => 1) == 1, "direct enumeration timing retains its status");
                try
                {
                    WordNativeTiming.Measure<int>("Revision.Next", () =>
                    {
                        throw enumerationError;
                    });
                }
                catch (InvalidOperationException error)
                {
                    observedError = error;
                }
            }

            check(object.ReferenceEquals(observedError, enumerationError), "enumeration diagnostics propagate the original exception");
            check(records.Count == 1 && records[0].Contains("calls=2") && records[0].Contains("max_member=Revision.Next"), "direct enumeration calls include failed attempts");
            check(!records[0].Contains("private"), "enumeration timing records names rather than provider data");
            Console.WriteLine("Word native timing: " + passed + " assertions; stage counts, privacy and unchanged results/errors; no editor access.");
            return passed;
        }
    }
}
