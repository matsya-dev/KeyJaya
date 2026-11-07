// The Framework's managed UIA focus tracker can throw before invoking our
// callback. Subscribe only in a disposable child; the resident owns its lifetime.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Automation;

namespace KeyJaya
{
    internal sealed class WordFocusNotice
    {
        public WordFocusNotice()
        {
        }

        public long Owner, Host;
        public int Process;
        public string Editor;
        public int[] Runtime;
        public double[] Bounds;
    }

    // Registration can deliver events before it returns, and provider reads can
    // complete out of order. Readiness must lead the stream; only the newest
    // event may publish a locator, including while registration is in progress.
    internal sealed class WordFocusOutput
    {
        readonly object gate = new object ();
        readonly TextWriter output;
        long revision;
        bool ready;
        string pending;
        internal WordFocusOutput(TextWriter output)
        {
            this.output = output;
        }

        internal long Begin()
        {
            lock (gate)
            {
                pending = null;
                return ++revision;
            }
        }

        internal bool Current(long eventRevision)
        {
            lock (gate)
                return revision == eventRevision;
        }

        internal void Publish(long eventRevision, WordFocusNotice notice)
        {
            string line = notice == null ? "clear" : new JavaScriptSerializer().Serialize(notice);
            lock (gate)
            {
                if (eventRevision != revision)
                    return;
                if (ready)
                    output.WriteLine(line);
                else
                    pending = line;
            }
        }

        internal void Ready()
        {
            lock (gate)
            {
                if (ready)
                    return;
                output.WriteLine("ready");
                ready = true;
                if (pending != null)
                    output.WriteLine(pending);
                pending = null;
            }
        }
    }

    internal sealed class FocusSession
    {
        int active = 1;
        public bool Active
        {
            get
            {
                return Interlocked.CompareExchange(ref active, 1, 1) != 0;
            }
        }

        public void Retire()
        {
            Interlocked.Exchange(ref active, 0);
        }
    }

    // A late UIA lookup from a dead helper must not republish its old editor.
    internal sealed class FocusObservation<T>
        where T : class
    {
        readonly object gate = new object ();
        FocusSession session;
        T value;
        public T Read()
        {
            lock (gate)
                return session != null && session.Active ? value : null;
        }

        public void Publish(FocusSession owner, T item, Func<T, T, bool> same = null)
        {
            lock (gate)
                if (owner.Active)
                {
                    if (session == owner && value != null && item != null && same != null && same(value, item))
                        return;
                    session = owner;
                    value = item;
                }
        }
    }

    // Publish locators without calling a provider on the pipe reader. Resolve
    // on one dedicated MTA worker, never on a caller's UI/STA thread. A slow
    // lookup cannot strand the pipe reader. Pending work coalesces to one slot.
    internal sealed class FocusLookup<T>
        where T : class
    {
        readonly Func<T> resolve;
        readonly Action<T> available;
        internal readonly object Identity;
        T value;
        int resolving;
        static readonly object queueGate = new object ();
        static readonly AutoResetEvent wake = new AutoResetEvent(false);
        static FocusLookup<T> pending;
        static FocusObservation<FocusLookup<T>> pendingSource;
        static bool started;
        internal FocusLookup(Func<T> resolve, object identity = null, Action<T> available = null)
        {
            this.resolve = resolve;
            Identity = identity;
            this.available = available;
        }

        internal static void Prepare(FocusObservation<FocusLookup<T>> source)
        {
            var lookup = source.Read();
            if (lookup == null || Interlocked.CompareExchange(ref lookup.value, null, null) != null)
                return;
            lock (queueGate)
            {
                if (lookup.resolving != 0 || !ReferenceEquals(source.Read(), lookup))
                    return;
                lookup.resolving = 1;
                if (pending != null)
                    Interlocked.Exchange(ref pending.resolving, 0);
                pending = lookup;
                pendingSource = source;
                if (!started)
                {
                    var worker = new Thread(ResolvePending)
                    {
                        IsBackground = true,
                        Name = "KeyJaya Word locator resolution"
                    };
                    worker.SetApartmentState(ApartmentState.MTA);
                    started = true;
                    worker.Start();
                }

                wake.Set();
            }
        }

        static void ResolvePending()
        {
            while (true)
            {
                wake.WaitOne();
                while (true)
                {
                    FocusLookup<T> lookup;
                    FocusObservation<FocusLookup<T>> source;
                    lock (queueGate)
                    {
                        lookup = pending;
                        source = pendingSource;
                        pending = null;
                        pendingSource = null;
                    }

                    if (lookup == null)
                        break;
                    try
                    {
                        // Word may briefly refuse a valid focus-event locator
                        // while rebuilding its accessibility tree. Retry only
                        // this read, before a still-current notice is discarded.
                        for (int attempt = 0; attempt < 3 && ReferenceEquals(source.Read(), lookup); attempt++)
                        {
                            T resolved = null;
                            try
                            {
                                resolved = lookup.resolve();
                            }
                            catch (Exception)
                            {
                            }

                            if (!ReferenceEquals(source.Read(), lookup))
                                break;
                            if (resolved != null)
                            {
                                Interlocked.Exchange(ref lookup.value, resolved);
                                if (lookup.available != null && ReferenceEquals(source.Read(), lookup))
                                    try
                                    {
                                        lookup.available(resolved);
                                    }
                                    catch (Exception)
                                    {
                                    }

                                break;
                            }

                            if (attempt < 2)
                                Thread.Sleep(20);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Interlocked.Exchange(ref lookup.resolving, 0);
                    }
                }
            }
        }

        internal static bool Invalidate(FocusObservation<FocusLookup<T>> source, T rejected)
        {
            var lookup = source.Read();
            if (lookup == null || rejected == null || !ReferenceEquals(Interlocked.CompareExchange(ref lookup.value, null, rejected), rejected))
                return false;
            Prepare(source);
            return true;
        }

        internal static bool Pending(FocusObservation<FocusLookup<T>> source)
        {
            var lookup = source.Read();
            return lookup != null && Volatile.Read(ref lookup.resolving) != 0 && ReferenceEquals(source.Read(), lookup);
        }

        internal static T ReadCurrent(FocusObservation<FocusLookup<T>> source)
        {
            var lookup = source.Read();
            if (lookup == null)
                return null;
            T found = Interlocked.CompareExchange(ref lookup.value, null, null);
            if (found == null)
            {
                Prepare(source);
                var clock = Stopwatch.StartNew();
                while (Volatile.Read(ref lookup.resolving) != 0 && clock.ElapsedMilliseconds < 150 && ReferenceEquals(source.Read(), lookup))
                    Thread.Sleep(5);
                found = Interlocked.CompareExchange(ref lookup.value, null, null);
            }

            return ReferenceEquals(source.Read(), lookup) ? found : null;
        }
    }

    internal sealed class WordFocusMonitor : IDisposable
    {
        internal const string Argument = "--watch-word-focus";
        readonly object gate = new object ();
        readonly ManualResetEvent stop = new ManualResetEvent(false);
        readonly Func<Process> start;
        readonly Action<FocusSession, WordFocusNotice> receive;
        readonly Action<string> log;
        readonly Thread worker;
        readonly Stopwatch clock = Stopwatch.StartNew();
        Process child;
        FocusSession session;
        long recoveryDue = -1, recoveryAfter, readyAfter;
        int disposed;
        internal WordFocusMonitor(Action<FocusSession, WordFocusNotice> receive, Action<string> log) : this(Start, receive, log)
        {
        }

        internal WordFocusMonitor(Func<Process> start, Action<FocusSession, WordFocusNotice> receive, Action<string> log)
        {
            this.start = start;
            this.receive = receive;
            this.log = log;
            worker = new Thread(Run)
            {
                IsBackground = true,
                Name = "KeyJaya isolated Word focus"
            };
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();
        }

        static Process Start()
        {
            return Process.Start(new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath, Argument) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 });
        }

        // A missing focus observation is not editing permission. Allow a brief
        // event-settling interval, then replace only our child if focus remains
        // unavailable. Repeated guards coalesce; healthy focus cancels the request.
        internal static long RecoveryDeadline(long now, long readyAfter, bool pending)
        {
            return Math.Max(now + (pending ? 3000 : 250), readyAfter);
        }

        internal void RequestRecovery(bool pending = false)
        {
            lock (gate)
                if (disposed == 0 && session != null && session.Active && recoveryDue < 0)
                    recoveryDue = Math.Max(RecoveryDeadline(clock.ElapsedMilliseconds, readyAfter, pending), recoveryAfter);
        }

        internal void CancelRecovery()
        {
            lock (gate)
                recoveryDue = -1;
        }

        bool Recover(FocusSession ticket)
        {
            lock (gate)
            {
                if (session != ticket || recoveryDue < 0 || clock.ElapsedMilliseconds < recoveryDue)
                    return false;
                recoveryDue = -1;
                recoveryAfter = clock.ElapsedMilliseconds + 5000;
                ticket.Retire();
                return true;
            }
        }

        void Report(string message)
        {
            try
            {
                if (log != null)
                    log(message);
            }
            catch (Exception)
            {
            }
        }

        internal static int RetryDelay(int failures)
        {
            return failures <= 1 ? 250 : failures == 2 ? 1000 : failures == 3 ? 5000 : 30000;
        }

        internal static int FailureCount(int previous, TimeSpan uptime)
        {
            // Occasional failures days apart must not accumulate a permanent
            // 30-second recovery delay. Back off only a rapid failure streak.
            return uptime >= TimeSpan.FromMinutes(1) ? 1 : previous >= 3 ? 4 : previous + 1;
        }

        internal static WordFocusNotice Decode(string line)
        {
            if (line == "clear")
                return null;
            if (string.IsNullOrEmpty(line) || line.Length > 4096)
                throw new InvalidDataException("Invalid focus notice.");
            var notice = new JavaScriptSerializer().Deserialize<WordFocusNotice>(line);
            if (notice == null || notice.Owner == 0 || notice.Host == 0 || notice.Process <= 0 || !WordCommentFocus.EditorId(notice.Editor) || notice.Editor.Length > 256 || notice.Runtime == null || notice.Runtime.Length < 2 || notice.Runtime.Length > 64)
                throw new InvalidDataException("Invalid focus locator.");
            return notice;
        }

        static string ReadLine(StreamReader input)
        {
            var text = new StringBuilder();
            int c;
            while ((c = input.Read()) != -1)
            {
                if (c == '\n')
                    return text.ToString().TrimEnd('\r');
                if (text.Length == 4096)
                    throw new InvalidDataException("Focus record too large.");
                text.Append((char)c);
            }

            if (text.Length != 0)
                throw new EndOfStreamException("Incomplete focus record.");
            return null;
        }

        void Run()
        {
            int failures = 0;
            while (!stop.WaitOne(0))
            {
                Process running = null;
                var ticket = new FocusSession();
                bool ready = false, refreshing = false;
                Stopwatch uptime = null;
                try
                {
                    running = start();
                    if (running == null)
                        throw new IOException("Focus helper did not start.");
                    running.Exited += (sender, args) => ticket.Retire();
                    running.EnableRaisingEvents = true;
                    lock (gate)
                    {
                        if (stop.WaitOne(0))
                            ticket.Retire();
                        else
                        {
                            child = running;
                            session = ticket;
                            recoveryDue = -1;
                        }
                    }

                    if (!ticket.Active)
                    {
                        if (stop.WaitOne(0))
                            break;
                        throw new IOException("Focus helper exited during startup.");
                    }

                    var input = running.StandardOutput;
                    var first = Task.Factory.StartNew(() => ReadLine(input));
                    var clock = Stopwatch.StartNew();
                    while (!first.Wait(50) && !stop.WaitOne(0) && ticket.Active && clock.ElapsedMilliseconds < 5000)
                    {
                    }

                    if (!first.IsCompleted || first.Result != "ready")
                        throw new IOException("Focus helper startup unavailable.");
                    ready = true;
                    uptime = Stopwatch.StartNew();
                    lock (gate)
                    {
                        // Startup catch-up and an owned-frame lookup can take
                        // longer than the caller's 150 ms wait. Do not retire
                        // their session before that bounded discovery can finish.
                        readyAfter = clock.ElapsedMilliseconds + 3000;
                        if (recoveryDue >= 0)
                            recoveryDue = Math.Max(recoveryDue, readyAfter);
                    }

                    Report("Word focus helper ready process=" + running.Id);
                    var next = Task.Factory.StartNew(() => ReadLine(input));
                    while (ticket.Active && !stop.WaitOne(0))
                    {
                        if (Recover(ticket))
                        {
                            refreshing = true;
                            Report("Word focus unavailable; refreshing only the focus helper; resident remains running.");
                            break;
                        }

                        if (!next.Wait(50))
                            continue;
                        string line = next.Result;
                        if (line == null)
                            break;
                        var notice = Decode(line);
                        if (notice != null)
                            CancelRecovery();
                        receive(ticket, notice);
                        next = Task.Factory.StartNew(() => ReadLine(input));
                    }

                    // Closing our child's pipe below completes this read. Observe
                    // disposal/read failures even when recovery did not await it.
                    next.ContinueWith(task =>
                    {
                        var ignored = task.Exception;
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (Exception error)
                {
                    if (!stop.WaitOne(0))
                        Report("Word focus helper unavailable: " + error.GetType().Name);
                }
                finally
                {
                    ticket.Retire();
                    lock (gate)
                    {
                        if (child == running)
                        {
                            child = null;
                            session = null;
                        }
                    }

                    Stop(running);
                }

                if (stop.WaitOne(0))
                    break;
                failures = refreshing ? 0 : FailureCount(failures, uptime == null ? TimeSpan.Zero : uptime.Elapsed);
                Report("Word focus helper " + (refreshing ? "refresh" : ready ? "exited" : "not ready") + "; retry in " + RetryDelay(failures) + " ms; resident remains running.");
                if (stop.WaitOne(RetryDelay(failures)))
                    break;
            }
        }

        static void Stop(Process running)
        {
            if (running == null)
                return;
            try
            {
                running.StandardInput.Close();
                if (!running.WaitForExit(250))
                {
                    running.Kill();
                    running.WaitForExit(250);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                running.Dispose();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
                return;
            stop.Set();
            Process running;
            lock (gate)
            {
                if (session != null)
                    session.Retire();
                running = child;
                child = null;
            }

            // Closing our child's pipe/terminating only our child releases a
            // blocked reader, even if its provider cannot unregister or return.
            if (running != null)
            {
                try
                {
                    running.StandardInput.Close();
                    if (!running.WaitForExit(250))
                        running.Kill();
                }
                catch (Exception)
                {
                }
            }

            if (worker.Join(1000))
                stop.Dispose();
        }

        [DllImport("kernel32.dll")]
        static extern uint SetErrorMode(uint mode);
        internal static int HelperMain()
        {
            int result = 1;
            var worker = new Thread(() => result = HelperRun());
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();
            worker.Join();
            return result;
        }

        static int HelperRun()
        {
            // A helper failure is reported by the supervisor, without a crash
            // dialog stealing keyboard focus. No process-wide policy is weakened
            // in the resident and no unhandled exception is resumed.
            SetErrorMode(3);
            using (var done = new ManualResetEvent(false))
            using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)))
            {
                output.AutoFlush = true;
                var notices = new WordFocusOutput(output);
                var parent = new Thread(() =>
                {
                    try
                    {
                        Console.OpenStandardInput().ReadByte();
                    }
                    catch (IOException)
                    {
                    }

                    try
                    {
                        done.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                })
                {
                    IsBackground = true
                };
                parent.Start();
                AutomationFocusChangedEventHandler handler = (sender, args) =>
                {
                    long revision = notices.Begin();
                    try
                    {
                        IntPtr owner = Native.GetForegroundWindow();
                        var notice = WordCommentFocus.Describe(owner, sender as AutomationElement);
                        notices.Publish(revision, notice);
                    }
                    catch (Exception)
                    {
                        // Failed resolution is a loss of evidence too. Do not
                        // let an older event's locator survive the newer event.
                        try
                        {
                            notices.Publish(revision, null);
                        }
                        catch (IOException)
                        {
                        }
                        catch (ObjectDisposedException)
                        {
                        }
                    }
                };
                bool registered = false;
                try
                {
                    // Seed the already-focused editor after a refresh, without
                    // requiring the user to leave/re-enter the comment. A newer
                    // real event always wins over this optional metadata probe.
                    long initial = notices.Begin();
                    Automation.AddAutomationFocusChangedEventHandler(handler);
                    registered = true;
                    notices.Ready();
                    if (notices.Current(initial))
                        notices.Publish(initial, WordCommentFocus.CurrentNotice());
                    done.WaitOne();
                    return 0;
                }
                catch (Exception)
                {
                    return 1;
                }
                finally
                {
                    if (registered)
                        try
                        {
                            Automation.RemoveAutomationFocusChangedEventHandler(handler);
                        }
                        catch (Exception)
                        {
                        }
                }
            }
        }
    }
}
