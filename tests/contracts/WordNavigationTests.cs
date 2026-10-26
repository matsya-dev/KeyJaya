// Run the resident's real font-ticket cleanup with a stalled release backend.
// No Word process, keyboard hook, clipboard or desktop input is used.
using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace KeyJaya
{
    internal static class WordNavigationTests
    {
        public static int Run()
        {
            int passed = 0, releases = 0, callerThread = 0, releaseThread = 0;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var resident = (Resident)FormatterServices.GetUninitializedObject(typeof(Resident));
            GC.SuppressFinalize(resident);
            object document = new object ();
            using (var entered = new ManualResetEventSlim())
            using (var allow = new ManualResetEventSlim())
            using (var finished = new ManualResetEventSlim())
            {
                var snapshot = new WordSelection.FontSnapshot(document, 5, 1, "raja", font =>
                {
                    releaseThread = Thread.CurrentThread.ManagedThreadId;
                    Interlocked.Increment(ref releases);
                    entered.Set();
                    allow.Wait();
                    finished.Set();
                });
                snapshot.Fonts.Add(new object ());
                var field = typeof(Resident).GetField("wordUndoFonts", flags);
                var clear = typeof(Resident).GetMethod("ClearWordUndoFonts", flags);
                field.SetValue(resident, snapshot);
                Exception navigationError = null;
                var caller = new Thread(() =>
                {
                    try
                    {
                        callerThread = Thread.CurrentThread.ManagedThreadId;
                        clear.Invoke(resident, null);
                    }
                    catch (Exception error)
                    {
                        navigationError = error;
                    }
                })
                {
                    IsBackground = true
                };
                caller.SetApartmentState(ApartmentState.STA);
                caller.Start();
                try
                {
                    if (!entered.Wait(3000))
                        throw new Exception("Font release backend was not reached");
                    passed++;
                    if (!caller.Join(200))
                        throw new Exception("Word navigation waits inside the keyboard hook for font cleanup");
                    if (navigationError != null)
                        throw navigationError;
                    passed++;
                    if (callerThread == releaseThread || field.GetValue(resident) != null)
                        throw new Exception("Native font release was not detached from navigation");
                    passed++;
                    if (snapshot.Matches(document, 5, 1, "raja") || snapshot.Fonts.Count != 0)
                        throw new Exception("Retired font ticket still authorizes another edit");
                    passed++;
                    bool refused = false;
                    try
                    {
                        snapshot.Acquire();
                    }
                    catch (InvalidOperationException)
                    {
                        refused = true;
                    }

                    if (!refused)
                        throw new Exception("Retired font ticket can be acquired again");
                    passed++;
                    snapshot.Dispose();
                    clear.Invoke(resident, null);
                    if (releases != 1)
                        throw new Exception("Retired fonts were released more than once");
                    passed++;
                }
                finally
                {
                    allow.Set();
                    if (!caller.Join(3000) || !finished.Wait(3000))
                        throw new Exception("Navigation cleanup fixture did not finish");
                }
            }

            int leasedReleases = 0;
            var leased = new WordSelection.FontSnapshot(document, 5, 1, "raja", font => Interlocked.Increment(ref leasedReleases));
            leased.Fonts.Add(new object ());
            leased.Acquire();
            leased.Retire();
            if (leasedReleases != 0 || leased.Matches(document, 5, 1, "raja"))
                throw new Exception("Retiring an in-use ticket released its leased fonts or retained ownership");
            passed++;
            leased.Release();
            leased.Dispose();
            if (leasedReleases != 1 || leased.Fonts.Count != 0)
                throw new Exception("Last active font lease did not release exactly once");
            passed++;
            Console.WriteLine("Word navigation cleanup: " + passed + " assertions; stalled native release does not block navigation.");
            return passed;
        }
    }
}
