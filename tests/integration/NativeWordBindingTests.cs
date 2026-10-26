// Opt-in native interoperability test. Owns one newly created Word process and
// scratch document; never enumerates, reads, selects or closes existing documents.
// No resident, hook, clipboard operation or OS keyboard injection is started.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KeyJaya
{
    public static class NativeWordBindingTests
    {
        static int passed;
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr thread);
        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr window, int command);
        static void Check(bool value, string message)
        {
            if (!value)
                throw new InvalidOperationException(message);
            passed++;
        }

        static object Get(object value, string property)
        {
            return value.GetType().InvokeMember(property, BindingFlags.GetProperty, null, value, null);
        }

        static void Set(object value, string property, object setting)
        {
            value.GetType().InvokeMember(property, BindingFlags.SetProperty, null, value, new[] { setting });
        }

        static object Call(object value, string method, params object[] args)
        {
            return value.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, value, args);
        }

        static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }

        static string Text(object document)
        {
            object content = Get(document, "Content");
            try
            {
                return ((string)Get(content, "Text")).TrimEnd('\r');
            }
            finally
            {
                Release(content);
            }
        }

        static void Prepare(object document, string input)
        {
            object content = Get(document, "Content"), font = null;
            try
            {
                Set(content, "Text", input);
                font = Get(content, "Font");
                Set(font, "Name", "Calibri");
                Set(font, "Size", 12f);
                Set(font, "Bold", 0);
                Set(font, "Italic", 0);
            }
            finally
            {
                Release(font);
                Release(content);
            }

            object english = Call(document, "Range", 0, 7);
            try
            {
                font = Get(english, "Font");
                Set(font, "Bold", -1);
                Set(font, "Size", 14f);
            }
            finally
            {
                Release(font);
                Release(english);
            }

            object range = Call(document, "Range", 0, input.Length);
            try
            {
                Call(range, "Select");
            }
            finally
            {
                Release(range);
            }
        }

        static string Fonts(object document, int length)
        {
            var signature = new StringBuilder();
            for (int n = 0; n < length; n++)
            {
                object range = Call(document, "Range", n, n + 1), font = null;
                try
                {
                    font = Get(range, "Font");
                    signature.Append(Get(font, "Name")).Append('|').Append(Get(font, "Size")).Append('|').Append(Get(font, "Bold")).Append('|').Append(Get(font, "Italic")).Append(';');
                }
                finally
                {
                    Release(font);
                    Release(range);
                }
            }

            return signature.ToString();
        }

        // Debugger-style fault injection pauses only the new test Word's UI
        // thread. A separate thread resumes it after the requested bounded delay,
        // independent of COM. The production app has no suspension code.
        // Never use this mechanism as production thread synchronization.
        // https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-suspendthread
        sealed class DelayedServer : IDisposable
        {
            readonly IntPtr handle;
            readonly Thread watchdog;
            int resumed;
            void Resume()
            {
                if (Interlocked.Exchange(ref resumed, 1) == 0)
                    ResumeThread(handle);
            }

            public DelayedServer(uint thread, int milliseconds)
            {
                handle = OpenThread(2, false, thread);
                Check(handle != IntPtr.Zero, "Open only the owned Word UI thread");
                uint previous = SuspendThread(handle);
                if (previous != 0)
                {
                    if (previous != uint.MaxValue)
                        ResumeThread(handle);
                    CloseHandle(handle);
                    throw new InvalidOperationException("Word UI thread was unavailable or already suspended");
                }

                watchdog = new Thread(() =>
                {
                    Thread.Sleep(milliseconds);
                    Resume();
                });
                watchdog.IsBackground = true;
                try
                {
                    watchdog.Start();
                }
                catch
                {
                    Resume();
                    CloseHandle(handle);
                    throw;
                }
            }

            public void Dispose()
            {
                Resume();
                watchdog.Join();
                CloseHandle(handle);
            }
        }

        static void Key(Native.CorrectionInputHold hold, IntPtr owner, uint vk, bool up)
        {
            Check(hold.Keyboard(new Native.KeyData { vk = vk, flags = up ? 0x80u : 0u }, up ? 0x101 : 0x100, owner), "Input remains queued during native binding");
        }

        static void ClosedTarget(IntPtr owner, IntPtr other, string scratch)
        {
            var hold = new Native.CorrectionInputHold();
            hold.Begin(owner);
            Key(hold, owner, 0xA0, false);
            Key(hold, owner, 0x4E, false);
            Key(hold, owner, 0x4E, true);
            Key(hold, owner, 0xA0, true);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(Resident);
            var resident = (Resident)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            GC.SuppressFinalize(resident);
            using (var timer = new System.Windows.Forms.Timer())
            {
                Action<string, object> set = (name, value) => type.GetField(name, flags).SetValue(resident, value);
                set("correctionInputHold", hold);
                set("correctionHoldOwner", owner);
                set("manualRetry", timer);
                set("correctionTyping", new CorrectionTyping());
                set("manualInputActive", true);
                set("logPath", Path.Combine(scratch, "closed-target.log"));
                Check((bool)type.GetMethod("RecoverClosedInputTarget", flags).Invoke(resident, null), "Real closed Word window releases resident input ownership");
                Check(!hold.Active && hold.Count == 0 && hold.Begin(other), "Abandoned queued input cannot be replayed into a different window");
                Check(hold.FinishIfEmpty(other), "New editor queue can immediately finish");
            }
        }

        static int PhysicalTyping(object document, IntPtr owner, uint thread, string scratch, int delay)
        {
            const string input = "Earlier untouched raja", suffix = " next Word";
            Prepare(document, input);
            object range = Call(document, "Range", input.Length, input.Length);
            try
            {
                Call(range, "Select");
            }
            finally
            {
                Release(range);
            }

            File.WriteAllText(Path.Combine(scratch, "physical-ready.txt"), "Tap Left Ctrl once in this scratch document and immediately type: next Word (including its leading space). The owned Word UI will pause for " + delay + " ms.");
            bool down = false, tapped = false;
            var wait = Stopwatch.StartNew();
            while (wait.Elapsed < TimeSpan.FromMinutes(10) && Native.IsWindow(owner))
            {
                bool current = (Native.GetAsyncKeyState(0xA2) & 0x8000) != 0;
                if (Native.GetForegroundWindow() == owner && current)
                    down = true;
                if (down && !current)
                {
                    tapped = Native.GetForegroundWindow() == owner;
                    break;
                }

                Thread.Sleep(1);
            }

            Check(tapped, "Physical Ctrl tap must occur in the owned scratch Word document");
            using (var pause = new DelayedServer(thread, delay))
                Thread.Sleep(delay + 50);
            wait.Restart();
            string text = "";
            while (wait.Elapsed < TimeSpan.FromSeconds(30))
            {
                text = Text(document);
                if (text.EndsWith(suffix, StringComparison.Ordinal))
                    break;
                Thread.Sleep(20);
            }

            Check(text.StartsWith("Earlier untouched ", StringComparison.Ordinal) && text.EndsWith(suffix, StringComparison.Ordinal), "Physical queued typing keeps every suffix character and preceding text");
            string converted = text.Substring(18, text.Length - 18 - suffix.Length);
            Check(new[] { "rāja", "rājā", "räja", "räjä" }.Contains(converted), "Resident converted the word without help from this test harness");
            object tail = Call(document, "Range", text.Length - suffix.Length, text.Length), font = null;
            try
            {
                font = Get(tail, "Font");
                Check(Convert.ToInt32(Get(font, "Italic")) == 0 && Convert.ToSingle(Get(font, "Size")) == 12f, "Physical following English text retains regular 12-point formatting");
            }
            finally
            {
                Release(font);
                Release(tail);
            }

            Check(new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3 }.All(key => (Native.GetAsyncKeyState(key) & 0x8000) == 0), "Physical modifiers are released after queue recovery");
            File.WriteAllText(Path.Combine(scratch, "physical-result.txt"), text + Environment.NewLine);
            Call(document, "Undo", 1);
            if (Text(document) != input)
                Call(document, "Undo", 1);
            Check(Text(document) == input, "Native undo restores the original after removing the following typing and conversion");
            Console.WriteLine("PASS: " + passed + " FAIL: 0; physical Ctrl conversion and immediate typing across a " + delay + " ms Word stall; no input injection or installed configuration changes.");
            return 0;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            string source = Path.GetFullPath(args[0]), scratch = Path.GetFullPath(args[1]);
            int delayMilliseconds = args.Length > 2 ? int.Parse(args[2]) : 350;
            Check(delayMilliseconds >= 350 && delayMilliseconds <= 10000, "Native server delay is bounded between 350 and 10000 ms");
            bool physical = args.Length > 3 && args[3] == "physical";
            Check(scratch.StartsWith(Path.Combine(source, "out") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(scratch), "Require fresh native-test fixture beneath out/");
            Directory.CreateDirectory(scratch);
            int[] existing = Process.GetProcessesByName("WINWORD").Select(process => process.Id).ToArray();
            File.WriteAllText(Path.Combine(scratch, "pre-existing-processes.txt"), string.Join(",", existing));
            object app = null, documents = null, document = null, retained = null;
            bool owned = false;
            IntPtr owner = IntPtr.Zero;
            uint pid = 0;
            short[] modifiers = new[]
            {
                0x10,
                0x11,
                0x12,
                0xA0,
                0xA1,
                0xA2,
                0xA3
            }.Select(key => (short)(Native.GetAsyncKeyState(key) & 0x8000)).ToArray();
            Check(modifiers.All(value => value == 0), "No physical modifier may be held before native tests");
            using (var other = new Form
            {
                Text = "KeyJaya native-test focus target",
                Width = 320,
                Height = 120
            }

            )
                try
                {
                    app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application", true));
                    documents = Get(app, "Documents");
                    Check(Convert.ToInt32(Get(documents, "Count")) == 0, "New Word application has no pre-existing documents");
                    document = Call(documents, "Add");
                    object window = Get(document, "ActiveWindow");
                    try
                    {
                        owner = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                    }
                    finally
                    {
                        Release(window);
                    }

                    uint thread = Native.GetWindowThreadProcessId(owner, out pid);
                    Check(pid != 0 && !existing.Contains((int)pid), "Word process is new and owned exclusively by this test");
                    owned = true;
                    File.WriteAllText(Path.Combine(scratch, "owned-process.txt"), pid.ToString());
                    Set(app, "DisplayAlerts", 0);
                    Set(document, "TrackRevisions", false);
                    Call(document, "SaveAs2", Path.Combine(scratch, "KeyJaya-native-binding.docx"));
                    Set(app, "Visible", true);
                    Call(app, "Activate");
                    Native.SetForegroundWindow(owner);
                    Check(Native.GetForegroundWindow() == owner, "Native test owns foreground; stop if focus cannot be acquired");
                    if (physical)
                        return PhysicalTyping(document, owner, thread, scratch, delayMilliseconds);
                    retained = WordApplication.ForWindow(owner);
                    Check(retained != null, "Bind to actual Word native object model");
                    const string input = "Earlier untouched raja and Krsna.", output = "Earlier untouched rāja and Kṛṣṇa.";
                    Prepare(document, input);
                    string originalFonts = Fonts(document, input.Length);
                    string englishFonts = Fonts(document, 18);
                    var hold = new Native.CorrectionInputHold();
                    Check(hold.Begin(owner), "Arm production input queue for native delay");
                    var watch = Stopwatch.StartNew();
                    using (var delay = new DelayedServer(thread, delayMilliseconds))
                    {
                        Key(hold, owner, 0xA0, false);
                        Key(hold, owner, 0x4E, false);
                        Key(hold, owner, 0x4E, true);
                        Key(hold, owner, 0xA0, true);
                        object bound = WordApplication.ForWindow(owner);
                        watch.Stop();
                        Check(bound != null, "Native COM binding recovers after bounded server delay");
                        Release(bound);
                    }

                    Check(watch.ElapsedMilliseconds >= delayMilliseconds - 200, "Test exercised a genuinely delayed native COM operation");
                    File.WriteAllText(Path.Combine(scratch, "binding-delay-ms.txt"), watch.ElapsedMilliseconds.ToString());
                    Check(hold.Take(other.Handle) == null && hold.Count == 4, "Different window cannot take delayed typing");
                    Native.Input[] queued = hold.Take(owner);
                    Check(queued.Length == 4 && queued[0].union.keyboard.vk == 0xA0 && queued[1].union.keyboard.vk == 0x4E && (queued[2].union.keyboard.flags & 2) != 0 && (queued[3].union.keyboard.flags & 2) != 0, "Queued key order retains the paired modifier release");
                    bool handled;
                    Check(WordSelection.Replace(owner, input, output, () => Native.GetForegroundWindow() == owner, out handled, new[] { new ItalicSpan(18, 4) }) && handled, "Native replacement with italics succeeds after delay");
                    Check(Text(document) == output && Fonts(document, 18) == englishFonts, "Earlier text and English font styling are preserved");
                    object converted = Call(document, "Range", 18, 22), convertedFont = null;
                    try
                    {
                        convertedFont = Get(converted, "Font");
                        Check(Convert.ToInt32(Get(convertedFont, "Italic")) == -1 && Convert.ToSingle(Get(convertedFont, "Size")) == 12f, "Only intended converted span receives italics at its original size");
                    }
                    finally
                    {
                        Release(convertedFont);
                        Release(converted);
                    }

                    Call(document, "Undo", 1);
                    Check(Text(document) == input && Fonts(document, input.Length) == originalFonts, "One native undo restores text and all original fonts");
                    // Change focus after at least one real patch. The production
                    // replacement must roll back the partial transaction in its own doc.
                    Prepare(document, input);
                    other.Show();
                    Call(app, "Activate");
                    Native.SetForegroundWindow(owner);
                    bool switched = false;
                    int contextCalls = 0;
                    IntPtr switchForeground = IntPtr.Zero;
                    Func<bool> context = () =>
                    {
                        contextCalls++;
                        if (!switched && Text(document) != input)
                        {
                            switched = true;
                            // Hide only the owned test frame: a minimized Word
                            // frame can immediately reactivate during COM calls.
                            // Verify actual focus loss before classifying a failure.
                            ShowWindow(owner, 0);
                            other.Activate();
                            Native.SetForegroundWindow(other.Handle);
                            switchForeground = Native.GetForegroundWindow();
                        }

                        return Native.GetForegroundWindow() == owner;
                    };
                    bool focusResult = WordSelection.Replace(owner, input, output, context, out handled, new[] { new ItalicSpan(18, 4) });
                    File.WriteAllText(Path.Combine(scratch, "focus-result.txt"), "result=" + focusResult + " switched=" + switched + " calls=" + contextCalls + " owner=" + owner + " other=" + other.Handle + " switch-foreground=" + switchForeground);
                    Check(switched && switchForeground != owner, "Harness must establish actual focus loss before evaluating rollback");
                    Check(!focusResult, "Focus change aborts an in-progress native edit; switched=" + switched + " calls=" + contextCalls + " foreground=" + switchForeground + " owner=" + owner);
                    Check(switched && Text(document) == input && Fonts(document, input.Length) == originalFonts, "Focus loss restores the complete original and formatting");
                    Call(app, "Quit", 0);
                    owned = false;
                    for (int n = 0; n < 100 && Native.IsWindow(owner); n++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(20);
                    }

                    Check(!Native.IsWindow(owner) && WordApplication.ForWindow(owner) == null, "Closed native window cannot bind to another Word instance");
                    int releases = 0;
                    object stale = retained;
                    retained = null;
                    Check(WordApplication.Resolve(() => stale, () => null, value => Get(value, "ActiveWindow") != null, value =>
                    {
                        releases++;
                        Release(value);
                    }) == null && releases == 1, "Disconnected real COM reference is rejected and released once");
                    ClosedTarget(owner, other.Handle, scratch);
                    Check(new[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3 }.All(key => (Native.GetAsyncKeyState(key) & 0x8000) == 0), "Physical modifiers remain released after native failure scenarios");
                    Console.WriteLine("PASS: " + passed + " FAIL: 0; native Word binding, bounded COM delay, formatting/undo, focus rollback, disconnection and closed-target queue recovery. No keyboard injection or installed profile used.");
                    return 0;
                }
                finally
                {
                    WordSelection.Trace = null;
                    try
                    {
                        if (owned)
                            Call(app, "Quit", 0);
                        else if (document != null && Native.IsWindow(owner))
                            Call(document, "Close", 0);
                    }
                    finally
                    {
                        Release(retained);
                        Release(document);
                        Release(documents);
                        Release(app);
                    }
                }
        }
    }
}
