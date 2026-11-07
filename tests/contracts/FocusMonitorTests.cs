// Real disposable pipe children, without UIA subscriptions, native input or Word.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace KeyJaya
{
    internal static class FocusMonitorTests
    {
        static int assertions;
        sealed class ParentPipeStream : MemoryStream
        {
            internal readonly ManualResetEvent Waiting = new ManualResetEvent(false);
            internal readonly ManualResetEvent Closed = new ManualResetEvent(false);
            internal ParentPipeStream(byte[] bytes) : base(bytes)
            {
            }

            public override int ReadByte()
            {
                int value = base.ReadByte();
                if (value >= 0)
                    return value;
                Waiting.Set();
                Closed.WaitOne();
                return -1;
            }
        }

        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception("Focus monitor: " + message);
            assertions++;
        }

        static bool Until(Func<bool> condition, int milliseconds = 5000)
        {
            var clock = Stopwatch.StartNew();
            while (!condition() && clock.ElapsedMilliseconds < milliseconds)
                Thread.Sleep(5);
            return condition();
        }

        static WordFocusNotice Notice()
        {
            return new WordFocusNotice
            {
                Owner = 41,
                Host = 73,
                Process = 91,
                Editor = "cardEditor_fixture",
                Runtime = new[]
                {
                    1,
                    2,
                    3
                }
            };
        }

        public static int Fixture(string mode)
        {
            if (mode == "exit")
                return 23;
            // Console.In retains a UTF-8 BOM on redirected input. The Process
            // writer emits one on UTF-8 Windows hosts, so decode the private
            // command pipe with BOM detection instead of losing its first command.
            var input = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, true);
            if (mode == "silent")
            {
                input.ReadLine();
                return 0;
            }

            Console.OutputEncoding = new UTF8Encoding(false);
            if (mode == "early")
            {
                var output = new WordFocusOutput(Console.Out);
                output.Publish(output.Begin(), Notice());
                output.Ready();
                Console.Out.Flush();
                input.ReadLine();
                return 0;
            }

            // Prepare the synthetic event before readiness: cold serializer/JIT
            // startup on a CI worker is not part of the settling-interval test.
            string encodedNotice = new JavaScriptSerializer().Serialize(Notice());
            Console.WriteLine("ready");
            Console.Out.Flush();
            if (mode == "bad")
            {
                Console.WriteLine(new string ('x', 5000));
                Console.Out.Flush();
                input.ReadLine();
                return 0;
            }

            if (mode == "stall")
            {
                Thread.Sleep(15000);
                return 0;
            }

            if (mode == "quiet")
            {
                string request;
                while ((request = input.ReadLine()) != null)
                    if (request == "notice")
                    {
                        Console.WriteLine(encodedNotice);
                        Console.Out.Flush();
                    }

                return 0;
            }

            Console.WriteLine(encodedNotice);
            Console.Out.Flush();
            string command;
            while ((command = input.ReadLine()) != null)
                if (command == "crash")
                    return 23;
            return 0;
        }

        static Process Start(string mode)
        {
            return Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--focus-fixture " + mode) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 });
        }

        static bool Alive(int id)
        {
            try
            {
                using (var p = Process.GetProcessById(id))
                    return !p.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static int Run()
        {
            assertions = 0;
            foreach (byte[] bytes in new[]
            {
                new byte[0],
                Encoding.UTF8.GetPreamble(),
                new byte[]
                {
                    239,
                    187,
                    191,
                    110,
                    111,
                    116,
                    105,
                    99,
                    101,
                    10
                }
            }

            )
                using (var pipe = new ParentPipeStream(bytes))
                using (var done = new ManualResetEvent(false))
                {
                    Exception failure = null;
                    var watcher = new Thread(() =>
                    {
                        try
                        {
                            Program.WaitForParent(pipe);
                        }
                        catch (Exception error)
                        {
                            failure = error;
                        }
                        finally
                        {
                            done.Set();
                        }
                    })
                    {
                        IsBackground = true
                    };
                    watcher.Start();
                    try
                    {
                        Check(pipe.Waiting.WaitOne(2000), "parent lease consumes every startup byte before awaiting EOF");
                        Check(!done.WaitOne(0), "an open parent pipe keeps the helper alive after a preamble or payload");
                        pipe.Closed.Set();
                        Check(watcher.Join(1000) && failure == null, "actual parent EOF releases the helper promptly");
                    }
                    finally
                    {
                        pipe.Closed.Set();
                        watcher.Join(1000);
                        pipe.Waiting.Dispose();
                        pipe.Closed.Dispose();
                    }
                }

            Check(WordFocusMonitor.FailureCount(0, TimeSpan.Zero) == 1, "first startup failure begins a retry streak");
            Check(WordFocusMonitor.FailureCount(3, TimeSpan.FromSeconds(59)) == 4, "rapid failures retain bounded backoff");
            Check(WordFocusMonitor.FailureCount(4, TimeSpan.FromMinutes(1)) == 1, "a healthy minute resets the next recovery delay");
            Check(WordFocusMonitor.FailureCount(int.MaxValue, TimeSpan.Zero) == 4, "failure streak cannot overflow");
            using (var output = new StringWriter())
            {
                var stream = new WordFocusOutput(output);
                long first = stream.Begin();
                Check(stream.Current(first), "initial catch-up snapshot starts with the current revision");
                stream.Publish(first, Notice());
                Check(output.ToString() == "", "focus received during registration stays buffered until readiness");
                long latest = stream.Begin();
                Check(!stream.Current(first) && stream.Current(latest), "a newer real event supersedes an unfinished catch-up snapshot");
                stream.Publish(latest, null);
                stream.Publish(first, Notice());
                stream.Ready();
                var lines = output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                Check(lines.Length == 2 && lines[0] == "ready" && lines[1] == "clear", "ready leads the stream and only the latest registration event follows");
                stream.Ready();
                Check(output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length == 2, "readiness is emitted only once");
                first = stream.Begin();
                latest = stream.Begin();
                stream.Publish(latest, Notice());
                stream.Publish(first, null);
                lines = output.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                Check(lines.Length == 3 && WordFocusMonitor.Decode(lines[2]).Editor == Notice().Editor, "late completion of an older event cannot clear the newer editor");
            }

            int earlyStarts = 0, earlyNotices = 0;
            using (var early = new WordFocusMonitor(() =>
            {
                Interlocked.Increment(ref earlyStarts);
                return Start("early");
            }, (session, notice) => Interlocked.Increment(ref earlyNotices), message =>
            {
            }))
            {
                Check(Until(() => Volatile.Read(ref earlyNotices) > 0), "an event during registration reaches the resident after ready");
                Check(earlyStarts == 1, "early registration events do not cause a helper restart");
            }

            string encoded = new JavaScriptSerializer().Serialize(Notice());
            Check(WordFocusMonitor.Decode(encoded).Editor == Notice().Editor, "metadata locator round-trips");
            Check(WordFocusMonitor.Decode("clear") == null, "non-comment focus clears observation");
            foreach (string invalid in new[]
            {
                "",
                "{}",
                "null",
                "not JSON",
                new string ('x', 4097),
                encoded.Replace("cardEditor_fixture", "Body"),
                encoded.Replace("\"Host\":73", "\"Host\":0")
            }

            )
            {
                bool rejected = false;
                try
                {
                    WordFocusMonitor.Decode(invalid);
                }
                catch (Exception)
                {
                    rejected = true;
                }

                Check(rejected, "invalid or oversized metadata rejected");
            }

            var mailbox = new FocusObservation<string>();
            var old = new FocusSession();
            mailbox.Publish(old, "old");
            old.Retire();
            Check(mailbox.Read() == null, "dead helper immediately loses focus ownership");
            var fresh = new FocusSession();
            mailbox.Publish(fresh, "fresh");
            mailbox.Publish(old, "late");
            Check(mailbox.Read() == "fresh", "late dead-helper completion cannot overwrite a fresh editor");
            fresh.Retire();
            var locators = new FocusObservation<FocusLookup<string>>();
            var locatorSession = new FocusSession();
            int lookups = 0;
            locators.Publish(locatorSession, new FocusLookup<string>(() => Interlocked.Increment(ref lookups) == 1 ? null : "editor"));
            Check(lookups == 0, "publishing metadata never calls a UI provider on the pipe reader");
            Check(FocusLookup<string>.ReadCurrent(locators) == "editor", "a transient locator failure retries before discarding the still-current notice");
            Check(lookups == 2, "a successful locator stops further retries immediately");
            Check(FocusLookup<string>.ReadCurrent(locators) == "editor" && lookups == 2, "resolved locator is reused only as a candidate for live guards");
            int missingReads = 0;
            var missingSource = new FocusObservation<FocusLookup<object>>();
            missingSource.Publish(new FocusSession(), new FocusLookup<object>(() =>
            {
                missingReads++;
                return null;
            }));
            Check(FocusLookup<object>.ReadCurrent(missingSource) == null && missingReads == 3, "persistent locator failure remains unavailable after exactly three bounded reads");
            int staleAvailable = 0;
            var staleSource = new FocusObservation<FocusLookup<object>>();
            var staleSession = new FocusSession();
            staleSource.Publish(staleSession, new FocusLookup<object>(() =>
            {
                staleSource.Publish(staleSession, null);
                return new object ();
            }, null, value => staleAvailable++));
            Check(FocusLookup<object>.ReadCurrent(staleSource) == null && staleAvailable == 0, "a superseded completion cannot cancel recovery through its availability callback");
            var callbackSource = new FocusObservation<FocusLookup<object>>();
            var callbackValue = new object ();
            callbackSource.Publish(new FocusSession(), new FocusLookup<object>(() => callbackValue, null, value =>
            {
                throw new IOException("fixture notification failure");
            }));
            Check(ReferenceEquals(FocusLookup<object>.ReadCurrent(callbackSource), callbackValue), "a notification failure cannot lose a successfully validated candidate");
            using (var ready = new ManualResetEvent(false))
            {
                int lateStarts = 0;
                using (var lateMonitor = new WordFocusMonitor(() =>
                {
                    Interlocked.Increment(ref lateStarts);
                    return Start("quiet");
                }, (session, notice) =>
                {
                }, message =>
                {
                    if (message.Contains("helper ready"))
                        ready.Set();
                }))
                {
                    Check(ready.WaitOne(4000), "late-resolution fixture helper reaches readiness");
                    var slowSource = new FocusObservation<FocusLookup<object>>();
                    var liveEditor = new object ();
                    slowSource.Publish(new FocusSession(), new FocusLookup<object>(() =>
                    {
                        Thread.Sleep(1100);
                        return liveEditor;
                    }, null, element => lateMonitor.CancelRecovery()));
                    Check(FocusLookup<object>.ReadCurrent(slowSource) == null, "a slow provider still respects the caller's bounded wait");
                    Check(FocusLookup<object>.Pending(slowSource), "a bounded caller timeout retains visible in-flight lookup state");
                    lateMonitor.RequestRecovery(FocusLookup<object>.Pending(slowSource));
                    Thread.Sleep(550);
                    Check(lateStarts == 1, "a live helper is not retired while the owned-frame lookup is still in flight");
                    Check(Until(() => ReferenceEquals(FocusLookup<object>.ReadCurrent(slowSource), liveEditor), 1800), "validated asynchronous success becomes available after a one-second provider read");
                    Check(lateStarts == 1 && !FocusLookup<object>.Pending(slowSource), "late validated success cancels recovery without another key press or helper restart");
                }
            }

            Check(WordFocusMonitor.RecoveryDeadline(5000, 3000, false) == 5250, "ordinary missing focus retains the short grace after startup");
            Check(WordFocusMonitor.RecoveryDeadline(5000, 3000, true) == 8000, "an in-flight frame locator receives a bounded three-second grace");
            Check(WordFocusMonitor.RecoveryDeadline(100, 3000, false) == 3000, "startup catch-up survives the short caller wait even before metadata arrives");
            Check(!FocusLookup<object>.Pending(new FocusObservation<FocusLookup<object>>()), "missing metadata is not an in-flight lookup");
            using (var entered = new ManualResetEvent(false))
            using (var release = new ManualResetEvent(false))
            {
                locators.Publish(locatorSession, new FocusLookup<string>(() =>
                {
                    entered.Set();
                    release.WaitOne();
                    return "late";
                }));
                string late = "unset";
                var resolver = new Thread(() => late = FocusLookup<string>.ReadCurrent(locators));
                resolver.Start();
                Check(entered.WaitOne(1000), "old provider lookup is deliberately stalled off the pipe reader");
                locators.Publish(locatorSession, new FocusLookup<string>(() => "new editor"));
                var bounded = Stopwatch.StartNew();
                Check(FocusLookup<string>.ReadCurrent(locators) == null && bounded.ElapsedMilliseconds < 500, "stalled provider work cannot block the caller or pipe reader indefinitely");
                release.Set();
                Check(resolver.Join(1000) && late == null, "newer focus invalidates an in-flight old lookup result");
                Check(FocusLookup<string>.ReadCurrent(locators) == "new editor", "new metadata resolves after the prior provider call returns");
            }

            var apartmentSource = new FocusObservation<FocusLookup<string>>();
            var apartmentSession = new FocusSession();
            string apartmentResult = null;
            ApartmentState providerApartment = ApartmentState.Unknown;
            var staCaller = new Thread(() =>
            {
                apartmentSource.Publish(apartmentSession, new FocusLookup<string>(() =>
                {
                    providerApartment = Thread.CurrentThread.GetApartmentState();
                    return "MTA editor";
                }));
                apartmentResult = FocusLookup<string>.ReadCurrent(apartmentSource);
            });
            staCaller.SetApartmentState(ApartmentState.STA);
            staCaller.Start();
            Check(staCaller.Join(1000) && apartmentResult == "MTA editor" && providerApartment == ApartmentState.MTA, "STA/UI callers never execute locator providers on their own thread");
            var originalLookup = apartmentSource.Read();
            apartmentSource.Publish(apartmentSession, new FocusLookup<string>(() => "duplicate"), (left, right) => true);
            Check(ReferenceEquals(apartmentSource.Read(), originalLookup), "duplicate metadata does not invalidate an in-flight/resolved locator");
            apartmentSource.Publish(apartmentSession, null, (left, right) => true);
            Check(apartmentSource.Read() == null, "duplicate coalescing never suppresses a real focus clear");
            apartmentSource.Publish(apartmentSession, originalLookup);
            var newApartmentSession = new FocusSession();
            var newLookup = new FocusLookup<string>(() => "new session");
            apartmentSource.Publish(newApartmentSession, newLookup, (left, right) => true);
            Check(ReferenceEquals(apartmentSource.Read(), newLookup), "duplicate metadata cannot retain a previous helper session");
            var replacing = new FocusObservation<FocusLookup<object>>();
            var replacingSession = new FocusSession();
            var oldEditor = new object ();
            var newEditor = new object ();
            object currentEditor = oldEditor;
            int replacementReads = 0;
            var replacingLookup = new FocusLookup<object>(() =>
            {
                Interlocked.Increment(ref replacementReads);
                return currentEditor;
            });
            replacing.Publish(replacingSession, replacingLookup);
            Check(ReferenceEquals(FocusLookup<object>.ReadCurrent(replacing), oldEditor), "initial provider resolves normally");
            currentEditor = newEditor;
            Check(FocusLookup<object>.Invalidate(replacing, oldEditor), "failed live validation invalidates the resolved provider object");
            Check(ReferenceEquals(FocusLookup<object>.ReadCurrent(replacing), newEditor) && replacementReads == 2, "replaced provider resolves without a new notice or helper/resident restart");
            Check(ReferenceEquals(replacing.Read(), replacingLookup), "provider replacement keeps the active metadata session");
            Check(!FocusLookup<object>.Invalidate(replacing, oldEditor) && ReferenceEquals(FocusLookup<object>.ReadCurrent(replacing), newEditor), "late invalidation cannot erase a newer provider result");
            replacingSession.Retire();
            Check(!FocusLookup<object>.Invalidate(replacing, newEditor) && FocusLookup<object>.ReadCurrent(replacing) == null, "retired metadata cannot restart or authorize a replacement lookup");
            locators.Publish(locatorSession, new FocusLookup<string>(() =>
            {
                locators.Publish(locatorSession, null);
                return "cleared";
            }));
            Check(FocusLookup<string>.ReadCurrent(locators) == null, "a focus clear invalidates a lookup completing in flight");
            locators.Publish(locatorSession, new FocusLookup<string>(() =>
            {
                locatorSession.Retire();
                return "dead helper";
            }));
            Check(FocusLookup<string>.ReadCurrent(locators) == null, "helper retirement invalidates a lookup completing in flight");
            int recoveryStarts = 0, recoveryNotices = 0, quietPid = 0;
            Process quietChild = null;
            var recoveryChildren = new List<int>();
            var recoveryLog = new List<string>();
            using (var recovery = new WordFocusMonitor(() =>
            {
                int attempt = Interlocked.Increment(ref recoveryStarts);
                var process = Start(attempt == 1 ? "quiet" : "notice");
                lock (recoveryChildren)
                    recoveryChildren.Add(process.Id);
                if (attempt == 1)
                {
                    quietChild = process;
                    Volatile.Write(ref quietPid, process.Id);
                }

                return process;
            }, (session, notice) => Interlocked.Increment(ref recoveryNotices), message =>
            {
                lock (recoveryLog)
                    recoveryLog.Add(message);
            }))
            {
                Check(Until(() =>
                {
                    lock (recoveryLog)
                        return recoveryLog.Exists(line => line.StartsWith("Word focus helper ready"));
                }), "alive but silent subscription is ready");
                recovery.RequestRecovery();
                recovery.CancelRecovery();
                Thread.Sleep(400);
                Check(recoveryStarts == 1 && Alive(quietPid), "healthy focus cancels a pending refresh without restarting the helper");
                // Reproduce UTF-8 Windows pipe input on ANSI hosts too. The
                // Process writer already emits its preamble when it has one.
                if (quietChild.StandardInput.Encoding.GetPreamble().Length == 0)
                {
                    byte[] preamble = Encoding.UTF8.GetPreamble();
                    quietChild.StandardInput.BaseStream.Write(preamble, 0, preamble.Length);
                    quietChild.StandardInput.BaseStream.Flush();
                }

                recovery.RequestRecovery();
                quietChild.StandardInput.WriteLine("notice");
                Check(Until(() => Volatile.Read(ref recoveryNotices) == 1), "a fresh event can repair focus during the settling interval");
                Thread.Sleep(350);
                Check(recoveryStarts == 1, "fresh metadata cancels unnecessary helper recovery");
                for (int repeat = 0; repeat < 30; repeat++)
                    recovery.RequestRecovery();
                Check(Until(() => Volatile.Read(ref recoveryNotices) == 2), "lost focus refreshes an alive helper and delivers new metadata without a resident restart");
                Check(recoveryStarts == 2 && !Alive(quietPid), "refresh stops only the owned silent helper");
                for (int repeat = 0; repeat < 30; repeat++)
                    recovery.RequestRecovery();
                Thread.Sleep(500);
                Check(recoveryStarts == 2, "repeated failed guards coalesce and respect the recovery cooldown");
                recovery.CancelRecovery();
                lock (recoveryLog)
                    Check(recoveryLog.Exists(line => line.Contains("refreshing only the focus helper")), "operational log identifies recovery without recording text");
            }

            lock (recoveryChildren)
                foreach (int id in recoveryChildren)
                    Check(!Alive(id), "refresh and shutdown leave no owned helper orphan");
            var children = new List<int>();
            Process current = null;
            FocusSession received = null;
            int starts = 0, notices = 0;
            using (var monitor = new WordFocusMonitor(() =>
            {
                int attempt = Interlocked.Increment(ref starts);
                var process = Start(attempt == 1 ? "exit" : "notice");
                lock (children)
                {
                    children.Add(process.Id);
                    current = process;
                }

                return process;
            }, (session, notice) =>
            {
                received = session;
                mailbox.Publish(session, notice == null ? null : notice.Editor);
                Interlocked.Increment(ref notices);
            }, message =>
            {
            }))
            {
                Check(Until(() => Volatile.Read(ref notices) >= 1), "early helper failure restarts and delivers fresh focus metadata");
                Check(starts >= 2 && mailbox.Read() == "cardEditor_fixture", "resident continues after a child exits before ready");
                var ticket = received;
                lock (children)
                    current.StandardInput.WriteLine("crash");
                Check(Until(() => !ticket.Active), "unexpected helper exit retires its cached evidence");
                Check(Until(() => Volatile.Read(ref notices) >= 2), "later failure also recovers without restarting the resident");
                mailbox.Publish(ticket, "stale result");
                Check(mailbox.Read() == "cardEditor_fixture", "failed helper cannot publish across recovery");
                monitor.Dispose();
                Check(mailbox.Read() == null, "shutdown retires the last observation");
            }

            lock (children)
                foreach (int id in children)
                    Check(!Alive(id), "only owned test helpers exit with their supervisor");
            int stalledId = 0;
            var stalled = new WordFocusMonitor(() =>
            {
                var process = Start("stall");
                Volatile.Write(ref stalledId, process.Id);
                return process;
            }, (session, notice) =>
            {
            }, message =>
            {
            });
            Check(Until(() => Volatile.Read(ref stalledId) != 0), "stalled test child started");
            var shutdown = Stopwatch.StartNew();
            stalled.Dispose();
            Check(shutdown.ElapsedMilliseconds < 1800, "a stuck child cannot block resident shutdown");
            Check(Until(() => !Alive(stalledId), 1500), "stuck child is terminated without leaving an orphan");
            foreach (string failureMode in new[]
            {
                "silent",
                "bad"
            }

            )
            {
                int attempts = 0, delivered = 0;
                var owned = new List<int>();
                using (var recovery = new WordFocusMonitor(() =>
                {
                    var process = Start(Interlocked.Increment(ref attempts) == 1 ? failureMode : "notice");
                    lock (owned)
                        owned.Add(process.Id);
                    return process;
                }, (session, notice) => Interlocked.Increment(ref delivered), message =>
                {
                }))
                {
                    Check(Until(() => Volatile.Read(ref delivered) > 0, 8000), "startup silence or oversized output cannot strand the supervisor: " + failureMode);
                    Check(attempts >= 2, "failed protocol child replaced without a resident restart: " + failureMode);
                }

                lock (owned)
                    foreach (int id in owned)
                        Check(!Alive(id), "failed protocol child and its replacement are cleaned up");
            }

            foreach (int failures in new[]
            {
                1,
                2,
                3,
                4,
                100,
                int.MaxValue
            }

            )
                Check(WordFocusMonitor.RetryDelay(failures) >= 250 && WordFocusMonitor.RetryDelay(failures) <= 30000, "restart backoff remains bounded");
            Console.WriteLine("Focus monitor isolation: " + assertions + " assertions; real child failures, recovery, stale evidence and bounded shutdown; no desktop UIA.");
            return assertions;
        }
    }
}
