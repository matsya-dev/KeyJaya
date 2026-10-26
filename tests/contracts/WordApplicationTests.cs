// Word instance-binding regressions. Fake applications expose ownership metadata
// only; no Word process, document, selection, clipboard or input hook is touched.
using System;
using System.Collections.Generic;

namespace KeyJaya
{
    public static class WordApplicationTests
    {
        sealed class Application
        {
            public readonly IntPtr Root;
            public readonly uint Process;
            public int Releases;
            public Application(long root, uint process)
            {
                Root = new IntPtr(root);
                Process = process;
            }
        }

        static void Check(bool condition, string message, ref int passed)
        {
            if (!condition)
                throw new InvalidOperationException("Word application binding: " + message);
            passed++;
        }

        static bool Matches(object value)
        {
            var app = (Application)value;
            return WordApplication.SameWindow(new IntPtr(42), 30292, app.Root, app.Process);
        }

        static void Release(object value)
        {
            ((Application)value).Releases++;
        }

        public static int Run()
        {
            int passed = 0;
            foreach (long expectedRoot in new long[]
            {
                0,
                1,
                42,
                0x100000001L
            }

            )
                foreach (long actualRoot in new long[]
                {
                    0,
                    1,
                    42,
                    0x100000001L
                }

                )
                    foreach (uint expectedProcess in new uint[]
                    {
                        0,
                        100,
                        30292
                    }

                    )
                        foreach (uint actualProcess in new uint[]
                        {
                            0,
                            100,
                            30292
                        }

                        )
                            Check(WordApplication.SameWindow(new IntPtr(expectedRoot), expectedProcess, new IntPtr(actualRoot), actualProcess) == (expectedRoot != 0 && expectedProcess != 0 && expectedRoot == actualRoot && expectedProcess == actualProcess), "both window and process must match", ref passed);
            var correct = new Application(42, 30292);
            var otherInstance = new Application(99, 100);
            int registeredCalls = 0;
            object found = WordApplication.Resolve(() => correct, () =>
            {
                registeredCalls++;
                return otherInstance;
            }, Matches, Release);
            Check(object.ReferenceEquals(found, correct) && registeredCalls == 0, "window-bound app wins without consulting stale global registration", ref passed);
            Check(correct.Releases == 0 && otherInstance.Releases == 0, "accepted reference transfers to caller", ref passed);
            Release(found);
            Check(correct.Releases == 1, "caller can release exactly its accepted reference", ref passed);
            foreach (Application rejected in new[]
            {
                new Application(99, 30292),
                new Application(42, 100),
                new Application(0, 30292),
                new Application(42, 0)
            }

            )
            {
                correct = new Application(42, 30292);
                found = WordApplication.Resolve(() => rejected, () => correct, Matches, Release);
                Check(object.ReferenceEquals(found, correct), "verified compatibility fallback remains available", ref passed);
                Check(rejected.Releases == 1 && correct.Releases == 0, "rejected reference released and accepted fallback retained", ref passed);
                Release(found);
            }

            var wrongWindow = new Application(43, 30292);
            found = WordApplication.Resolve(() => otherInstance, () => wrongWindow, Matches, Release);
            Check(found == null && otherInstance.Releases == 1 && wrongWindow.Releases == 1, "wrong process and wrong window both rejected", ref passed);
            correct = new Application(42, 30292);
            found = WordApplication.Resolve(() => null, () => correct, Matches, Release);
            Check(object.ReferenceEquals(found, correct), "missing native interface uses verified registered app", ref passed);
            Release(found);
            found = WordApplication.Resolve(() =>
            {
                throw new InvalidOperationException();
            }, () => null, Matches, Release);
            Check(found == null, "native API failure remains quiet when no safe fallback exists", ref passed);
            correct = new Application(42, 30292);
            found = WordApplication.Resolve(() =>
            {
                throw new InvalidOperationException();
            }, () => correct, Matches, Release);
            Check(object.ReferenceEquals(found, correct) && correct.Releases == 0, "native API failure does not skip safe compatibility lookup", ref passed);
            Release(found);
            otherInstance = new Application(99, 100);
            found = WordApplication.Resolve(() => otherInstance, () =>
            {
                throw new InvalidOperationException();
            }, value =>
            {
                throw new InvalidOperationException();
            }, Release);
            Check(found == null && otherInstance.Releases == 1, "disconnected candidate is released when verification throws", ref passed);
            correct = new Application(42, 30292);
            uint currentProcess = 100;
            found = WordApplication.Resolve(() => correct, () => null, value => currentProcess == 30292 && Matches(value), Release);
            Check(found == null && correct.Releases == 1, "changed owner cannot authorize a stale candidate", ref passed);
            // A later operation must acquire its own application. No accepted or
            // rejected COM reference may survive as an implicit cross-window cache.
            int acquisitions = 0;
            var acquired = new List<Application>();
            for (int n = 0; n < 20; n++)
            {
                found = WordApplication.Resolve(() =>
                {
                    acquisitions++;
                    var app = new Application(42, 30292);
                    acquired.Add(app);
                    return app;
                }, () => null, Matches, Release);
                Check(found != null && ((Application)found).Releases == 0, "fresh operation owns a fresh reference", ref passed);
                Release(found);
            }

            Check(acquisitions == 20 && acquired.TrueForAll(app => app.Releases == 1), "no retained references after repeated operations", ref passed);
            Console.WriteLine("Word application binding: " + passed + " assertions; multiple instances, stale registration, fallback and reference ownership.");
            return passed;
        }
    }
}
