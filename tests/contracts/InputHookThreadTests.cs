// The real thread owner/message-pump lifecycle against an injected queue. Never
// installs global hooks or sends input. The caller may block without starving it.
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace KeyJaya
{
    internal static class InputHookThreadTests
    {
        sealed class FocusLease : IDisposable
        {
            public int Releases;
            public void Dispose()
            {
                Releases++;
            }
        }

        sealed class Backend : IInputHookBackend
        {
            readonly AutoResetEvent signal = new AutoResetEvent(false);
            readonly ConcurrentQueue<Action> work = new ConcurrentQueue<Action>();
            volatile bool stop;
            public int Installed, Pumped, Released, ReleaseCount, InstallCount, PumpCount;
            public bool Fail, FailFirstPump, ReturnFirstPump, FailFirstRetry;
            public IntPtr Keyboard
            {
                get
                {
                    return new IntPtr(1);
                }
            }

            public IntPtr Mouse
            {
                get
                {
                    return new IntPtr(2);
                }
            }

            public void Install(Native.Hook keyboard, Native.Hook mouse)
            {
                Installed = Thread.CurrentThread.ManagedThreadId;
                int attempt = Interlocked.Increment(ref InstallCount);
                if (Fail || FailFirstRetry && attempt == 2)
                    throw new InvalidOperationException("Injected registration failure");
            }

            public void Post(Action action)
            {
                work.Enqueue(action);
                signal.Set();
            }

            public void Pump()
            {
                Pumped = Thread.CurrentThread.ManagedThreadId;
                int attempt = Interlocked.Increment(ref PumpCount);
                if (attempt == 1 && FailFirstPump)
                    throw new InvalidOperationException("Injected message-loop failure");
                if (attempt == 1 && ReturnFirstPump)
                    return;
                while (!stop)
                {
                    signal.WaitOne();
                    Action action;
                    while (work.TryDequeue(out action))
                        action();
                }
            }

            public void Quit()
            {
                stop = true;
                signal.Set();
            }

            public void Release()
            {
                Released = Thread.CurrentThread.ManagedThreadId;
                ReleaseCount++;
            }
        }

        public static int Run()
        {
            int assertions = 0;
            Action<bool, string> check = (ok, message) =>
            {
                if (!ok)
                    throw new Exception("Input thread: " + message);
                assertions++;
            };
            int caller = Thread.CurrentThread.ManagedThreadId, processed = 0;
            var backend = new Backend();
            Native.Hook hook = (code, w, l) => IntPtr.Zero;
            using (var completed = new ManualResetEvent(false))
            using (var owner = new InputHookThread(backend, hook, hook))
            {
                check(owner.Keyboard == new IntPtr(1) && owner.Mouse == new IntPtr(2), "both hooks transfer to their owner");
                backend.Post(() =>
                {
                    Interlocked.Increment(ref processed);
                    completed.Set();
                });
                // No pumping on this caller: emulate a UI/automation stall.
                check(completed.WaitOne(500), "hook work runs while the UI caller waits");
                check(processed == 1 && backend.Installed != caller && backend.Pumped == backend.Installed, "installation and dispatch share the dedicated thread");
                owner.Dispose();
                check(backend.Released == backend.Installed && backend.ReleaseCount == 1, "unhook occurs once on the installing thread");
            }

            check(backend.ReleaseCount == 1, "repeated disposal does not repeat unhook");
            int directLogThread = 0;
            string directMessage = null;
            InputHookThread.WriteLog(message =>
            {
                directLogThread = Thread.CurrentThread.ManagedThreadId;
                directMessage = message;
            }, "ordinary caller");
            check(directLogThread == caller && directMessage == "ordinary caller", "non-hook logging stays synchronous on its caller");
            using (var logEntered = new ManualResetEvent(false))
            using (var allowLog = new ManualResetEvent(false))
            using (var logFinished = new ManualResetEvent(false))
            using (var returned = new ManualResetEvent(false))
            using (var nextInput = new ManualResetEvent(false))
            using (var failedLog = new ManualResetEvent(false))
            {
                var loggingBackend = new Backend();
                int logThread = 0;
                string loggedMessage = null;
                using (var owner = new InputHookThread(loggingBackend, hook, hook))
                    try
                    {
                        loggingBackend.Post(() =>
                        {
                            InputHookThread.WriteLog(message =>
                            {
                                logThread = Thread.CurrentThread.ManagedThreadId;
                                loggedMessage = message;
                                logEntered.Set();
                                allowLog.WaitOne();
                                logFinished.Set();
                            }, "hook diagnostic");
                            returned.Set();
                        });
                        check(returned.WaitOne(500) && logEntered.WaitOne(500), "a stalled diagnostic writer does not hold the input callback");
                        check(logThread != loggingBackend.Pumped && loggedMessage == "hook diagnostic", "callback diagnostics retain their message on a separate thread");
                        loggingBackend.Post(() => nextInput.Set());
                        check(nextInput.WaitOne(500), "later input still dispatches while diagnostic I/O is stalled");
                        loggingBackend.Post(() => InputHookThread.WriteLog(message =>
                        {
                            failedLog.Set();
                            throw new InvalidOperationException("fixture logger failed");
                        }, "failed diagnostic"));
                        check(failedLog.WaitOne(500), "a queued logger failure is contained off the hook thread");
                    }
                    finally
                    {
                        allowLog.Set();
                        logFinished.WaitOne(1000);
                    }
            }

            var broken = new Backend
            {
                Fail = true
            };
            bool failed = false;
            try
            {
                using (var owner = new InputHookThread(broken, hook, hook))
                {
                }
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }

            check(failed && broken.ReleaseCount == 1, "failed registration is reported and partial hooks cleaned up");
            foreach (bool returns in new[]
            {
                false,
                true
            }

            )
                using (var recovered = new ManualResetEvent(false))
                using (var logEntered = new ManualResetEvent(false))
                using (var allowLog = new ManualResetEvent(false))
                {
                    var recovering = new Backend
                    {
                        FailFirstPump = !returns,
                        ReturnFirstPump = returns,
                        FailFirstRetry = !returns
                    };
                    try
                    {
                        using (var owner = new InputHookThread(recovering, hook, hook, message =>
                        {
                            logEntered.Set();
                            allowLog.WaitOne();
                        }))
                        {
                            check(owner.Keyboard != IntPtr.Zero && owner.Mouse != IntPtr.Zero, "startup proof survives an immediate dispatch failure");
                            var clock = System.Diagnostics.Stopwatch.StartNew();
                            while (Volatile.Read(ref recovering.PumpCount) < 2 && clock.ElapsedMilliseconds < 4000)
                                Thread.Sleep(5);
                            check(recovering.PumpCount == 2 && recovering.InstallCount == (returns ? 2 : 3), "unexpected return or failure recovers, including failed re-registration");
                            recovering.Post(() => recovered.Set());
                            check(logEntered.WaitOne(500) && recovered.WaitOne(500), "dispatch recovers even while the status logger is stalled");
                            check(recovering.Installed == recovering.Pumped && recovering.Released == recovering.Installed && recovering.Installed != caller, "recovery and prior cleanup retain installing-thread affinity");
                            owner.Dispose();
                            check(recovering.ReleaseCount == recovering.InstallCount, "every recovery attempt releases its own hooks exactly once");
                            int installations = recovering.InstallCount;
                            Thread.Sleep(300);
                            check(recovering.InstallCount == installations, "shutdown does not restart the hooks");
                        }
                    }
                    finally
                    {
                        allowLog.Set();
                    }
                }

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var resident = (Resident)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Resident));
            GC.SuppressFinalize(resident);
            var focusLease = new FocusLease();
            var residentBackend = new Backend();
            using (var owner = new InputHookThread(residentBackend, hook, hook))
            {
                typeof(Resident).GetField("inputHooks", flags).SetValue(resident, owner);
                typeof(Resident).GetField("wordCommentFocus", flags).SetValue(resident, focusLease);
                var stop = typeof(Resident).GetMethod("StopInput", flags);
                stop.Invoke(resident, null);
                stop.Invoke(resident, null);
                check(residentBackend.ReleaseCount == 1 && focusLease.Releases == 1, "startup rollback and shutdown retire their hooks and helper exactly once");
                check((bool)typeof(Resident).GetField("closing", flags).GetValue(resident), "cleanup makes late callbacks pass input through");
                check(typeof(Resident).GetField("inputHooks", flags).GetValue(resident) == null && typeof(Resident).GetField("wordCommentFocus", flags).GetValue(resident) == null, "cleanup clears ownership without needing a form handle");
            }

            Console.WriteLine("Input hook thread: " + assertions + " assertions; independent dispatch, affinity and cleanup; no desktop hooks.");
            return assertions;
        }
    }
}
