// Opt-in integration checks in a new, exclusively owned local Word document.
// Uses the running, hash-verified development resident's normal hotkey dispatch.
// Input is limited to the owned Modern Comments editor; no manuscript is opened.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace KeyJaya
{
    public static class ModernWordCommentTests
    {
        static int passed;
        static IntPtr owner, resident;
        static AutomationElement editor;
        static object app, document, documents;
        static string original, residentLog;
        static string setupStage;
        const string NewCommentCommand = "ReviewNewComment";
        static uint testedResident;
        static int silentHelper;
        static readonly object faultGate = new object ();
        static IntPtr silentHandle;
        static Timer faultDeadline;
        static int faultTimedOut;
        static bool recoveryScope;
        static string scope, panelAnchor;
        static IntPtr panelHost;
        static ModernWordPanel panel;
        static readonly List<object> results = new List<object>();
        static readonly List<string> completedKeys = new List<string>();
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint access, bool inherit, uint process);
        [DllImport("kernel32.dll")]
        static extern bool TerminateProcess(IntPtr process, uint code);
        static Process VerifiedHelper(int id)
        {
            Check(id > 0 && id != testedResident, "Fault injection cannot target the resident");
            using (var residentProcess = Process.GetProcessById((int)testedResident))
            using (var query = new ManagementObjectSearcher("SELECT ParentProcessId, ExecutablePath, CommandLine FROM Win32_Process WHERE ProcessId=" + id))
            using (var rows = query.Get())
            {
                Check(rows.Count == 1, "One live focus helper is required");
                foreach (ManagementObject row in rows)
                    using (row)
                        Check(Convert.ToUInt32(row["ParentProcessId"]) == testedResident && string.Equals(Convert.ToString(row["ExecutablePath"]), residentProcess.MainModule.FileName, StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(Convert.ToString(row["CommandLine"]), @"(?:^|\s)--watch-word-focus(?:\s|$)"), "Fault injection targets only the verified resident's own focus helper");
            }

            return Process.GetProcessById(id);
        }

        static void VerifySilentHelper()
        {
            Check(Native.GetForegroundWindow() == owner, "Scratch Word owns focus before helper fault injection");
            silentHelper = ModernWordSetup.LoggedHelper(ModernWordSetup.ReadLog(residentLog));
            using (var helper = VerifiedHelper(silentHelper))
            {
                string marker = Path.Combine(Path.GetDirectoryName(helper.MainModule.FileName), "silent-focus-helper.pid");
                Check(File.Exists(marker) && File.ReadAllText(marker).Trim() == silentHelper.ToString(), "Recovery scope requires the explicitly prepared silent-child fixture; it never pauses a real UIA client");
                // Keep an exact process handle for cleanup, including when a
                // provider call in the fixture cannot return. The deadline
                // releases only our fault, never another process or document.
                silentHandle = OpenProcess(1, false, (uint)silentHelper);
                Check(silentHandle != IntPtr.Zero, "Retain the verified helper for bounded fault cleanup");
                faultDeadline = new Timer(ignored =>
                {
                    Interlocked.Exchange(ref faultTimedOut, 1);
                    CleanupFault();
                }, null, 15000, Timeout.Infinite);
            }
        }

        static void WaitRecovery()
        {
            for (int poll = 0; poll < 160; poll++)
            {
                Check(Native.GetForegroundWindow() == owner && WordCommentFocus.Editable(editor), "Do not refocus the scratch editor during recovery");
                string log = ModernWordSetup.ReadLog(residentLog);
                int helperIdentity = ModernWordSetup.LoggedHelper(log);
                if (helperIdentity != silentHelper && log.Contains("refreshing only the focus helper") && ModernWordSetup.RecoveredLocator(log, helperIdentity))
                {
                    using (var helper = VerifiedHelper(helperIdentity))
                        Check(!helper.HasExited, "Replacement focus helper is alive");
                    using (var residentProcess = Process.GetProcessById((int)testedResident))
                        Check(!residentProcess.HasExited, "Resident PID is unchanged after silent-helper recovery");
                    Check(Volatile.Read(ref faultTimedOut) == 0, "Recovery completed before emergency fault cleanup");
                    return;
                }

                Thread.Sleep(50);
            }

            throw new InvalidOperationException("Alive silent helper did not recover without a resident restart");
        }

        static void CleanupFault()
        {
            lock (faultGate)
            {
                if (faultDeadline != null)
                    faultDeadline.Dispose();
                if (silentHandle == IntPtr.Zero)
                    return;
                TerminateProcess(silentHandle, 29);
                CloseHandle(silentHandle);
                silentHandle = IntPtr.Zero;
            }
        }

        static object Get(object value, string property)
        {
            return value.GetType().InvokeMember(property, BindingFlags.GetProperty, null, value, null);
        }

        static object Call(object value, string method, params object[] args)
        {
            return value.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, value, args);
        }

        static void Set(object value, string property, object setting)
        {
            value.GetType().InvokeMember(property, BindingFlags.SetProperty, null, value, new[] { setting });
        }

        static void Free(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }

        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            passed++;
        }

        static string Body()
        {
            object content = Get(document, "Content");
            try
            {
                return Convert.ToString(Get(content, "Text"));
            }
            finally
            {
                Free(content);
            }
        }

        static AutomationElement Focus()
        {
            return WordCommentFocus.Focused(owner);
        }

        static TextPattern Pattern()
        {
            return (TextPattern)editor.GetCurrentPattern(TextPattern.Pattern);
        }

        static string Text()
        {
            return Pattern().DocumentRange.GetText(-1).TrimEnd('\r');
        }

        static void WaitIdle()
        {
            Check(ModernWordSetup.Converts(scope), "Setup-only scopes never use the resident settlement probe");
            // Text arrives before selection retention and held-input settlement.
            // Query the existing diagnostic queue probe rather than racing them.
            var probe = typeof(LaunchRequests).GetMethod("SendMessageTimeout", BindingFlags.Static | BindingFlags.NonPublic);
            for (int poll = 0; poll < 100; poll++)
            {
                object[] args =
                {
                    resident,
                    (uint)(0x8000 + 35),
                    IntPtr.Zero,
                    IntPtr.Zero,
                    (uint)2,
                    (uint)500,
                    UIntPtr.Zero
                };
                if ((IntPtr)probe.Invoke(null, args) != IntPtr.Zero && (UIntPtr)args[6] == UIntPtr.Zero)
                    return;
                Thread.Sleep(50);
            }

            throw new InvalidOperationException("Resident did not settle the owned conversion");
        }

        static AutomationElement WaitEditor()
        {
            for (int poll = 0; poll < 100; poll++)
            {
                Check(OwnedScratch(), "Only the owned scratch window may discover a comment editor");
                var focus = Focus();
                if (focus != null && WordCommentFocus.InComments(owner, focus) && WordCommentFocus.Editable(focus))
                    return focus;
                // Setup must not depend on the notification channel under test.
                // Resolve only a unique, currently focused editor in our own
                // scratch frame; never focus an inferred or stale candidate.
                uint candidateProcess;
                Native.GetWindowThreadProcessId(owner, out candidateProcess);
                var identity = Native.FocusIdentity();
                Check(identity != null && OwnedScratch(), "Scratch native focus is available for independent discovery");
                IntPtr host = new IntPtr((long)(uint)identity[4] | ((long)identity[5] << 32));
                uint hostProcess;
                Native.GetWindowThreadProcessId(host, out hostProcess);
                var hostClass = new System.Text.StringBuilder(64);
                Native.GetClassName(host, hostClass, hostClass.Capacity);
                Check(hostProcess == candidateProcess && WordPasteOptions.Owner(host) == owner && WordCommentFocus.HostClass(hostClass.ToString()), "Only the owned native comment host may be inspected");
                var ownedEditors = AutomationElement.FromHandle(host).FindAll(TreeScope.Element | TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, (int)candidateProcess), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
                var candidate = ModernWordSetup.FindFocused(ownedEditors.Cast<AutomationElement>(), value => value.Current.HasKeyboardFocus && WordCommentFocus.EditorId(value.Current.AutomationId) && WordCommentFocus.InComments(owner, value) && WordCommentFocus.Editable(value), () => OwnedScratch() && EditGuard.SameFocus(identity, Native.FocusIdentity()));
                if (candidate != null)
                    return candidate;
                Thread.Sleep(50);
            }

            var last = Focus() ?? editor;
            if (last == null && Native.GetForegroundWindow() == owner)
                last = AutomationElement.FocusedElement;
            var chain = new List<object>();
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            var candidates = new List<object>();
            // Failure evidence is limited to our exclusively owned scratch
            // Word process. Do not read editor Name/Value/Text here.
            if (Native.GetForegroundWindow() == owner)
            {
                var edits = AutomationElement.FromHandle(owner).FindAll(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, (int)process), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
                foreach (AutomationElement candidate in edits)
                    if ((WordCommentFocus.EditorId(candidate.Current.AutomationId) || candidate.Current.HasKeyboardFocus || candidate.Current.AutomationId.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0) && candidates.Count < 16)
                        candidates.Add(new { id = candidate.Current.AutomationId, focus = candidate.Current.HasKeyboardFocus, enabled = candidate.Current.IsEnabled, comment = WordCommentFocus.InComments(owner, candidate), editable = WordCommentFocus.Editable(candidate) });
            }

            for (int depth = 0; depth < 16 && last != null && last.Current.ProcessId == (int)process; depth++, last = TreeWalker.RawViewWalker.GetParent(last))
            {
                var nativeClass = new System.Text.StringBuilder(64);
                Native.GetClassName(new IntPtr(last.Current.NativeWindowHandle), nativeClass, nativeClass.Capacity);
                chain.Add(new { id = last.Current.AutomationId, cls = last.Current.ClassName, native = nativeClass.ToString(), focus = last.Current.HasKeyboardFocus });
            }

            throw new InvalidOperationException("No focused writable Modern Comments editor: " + new JavaScriptSerializer().Serialize(new { chain = chain, candidates = candidates }));
        }

        static void WaitText(string expected, bool settle = true)
        {
            bool found = ModernWordSetup.Wait(OwnedScratch, () => Text() == expected, () => Thread.Sleep(50), 160, 2);
            var json = new JavaScriptSerializer();
            Check(found, found ? "Prepared comment text matches" : "Comment text mismatch; expected=" + json.Serialize(expected) + "; observed=" + json.Serialize(Text()));
            if (settle && ModernWordSetup.Converts(scope))
                WaitIdle();
        }

        static void Seed(string text)
        {
            setupStage = "seed-input";
            Check(OwnedScratch() && WordCommentFocus.InComments(owner, editor), "Only the owned scratch comment may acquire setup focus");
            editor.SetFocus();
            editor = WaitEditor();
            string identity = editor.Current.AutomationId;
            Func<bool> current = () => OwnedScratch() && editor.Current.AutomationId == identity && WordCommentFocus.Editable(editor);
            ModernWordSetup.Seed(text, recoveryScope, current, value =>
            {
                ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
                editor = WaitEditor();
                Check(editor.Current.AutomationId == identity, "Seeding retains the same logical scratch comment");
            }, value => Check(current() && Native.TypeText(value), "Type prepared text only into the verified scratch comment"), value => WaitText(value, false), () =>
            {
                var end = Pattern().DocumentRange;
                end.MoveEndpointByRange(TextPatternRangeEndpoint.Start, end, TextPatternRangeEndpoint.End);
                end.Select();
                if (object.Equals(Pattern().GetSelection()[0].GetAttributeValue(TextPattern.IsItalicAttribute), true))
                    Check(current() && Native.Chord(0x49, true), "Initialize owned fixture typing style");
            }, FixtureReady, () => Thread.Sleep(50));
            TextPatternRange originalCaret = null;
            ModernWordSetup.SetupUndoBoundary(recoveryScope, current, () =>
            {
                var selection = Pattern().GetSelection();
                Check(selection.Length == 1 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) == 0, "Setup starts with one collapsed scratch caret");
                originalCaret = selection[0].Clone();
                var key = typeof(Native).GetMethod("Key", BindingFlags.Static | BindingFlags.NonPublic);
                var navigation = new[]
                {
                    (Native.Input)key.Invoke(null, new object[] { (ushort)0x25, false }),
                    (Native.Input)key.Invoke(null, new object[] { (ushort)0x25, true }),
                    (Native.Input)key.Invoke(null, new object[] { (ushort)0x27, false }),
                    (Native.Input)key.Invoke(null, new object[] { (ushort)0x27, true })
                };
                Check(current() && Native.SendInput((uint)navigation.Length, navigation, Marshal.SizeOf(typeof(Native.Input))) == navigation.Length, "Separate setup typing from conversion undo only in the scratch editor");
            }, () =>
            {
                var selection = Pattern().GetSelection();
                return Text() == text && selection.Length == 1 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, originalCaret, TextPatternRangeEndpoint.Start) == 0 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.End, originalCaret, TextPatternRangeEndpoint.End) == 0;
            }, () => Thread.Sleep(50));
        }

        static bool FixtureReady()
        {
            bool modern;
            var observed = WordCommentFocus.Focused(owner, out modern);
            return modern && observed != null && EditGuard.SameFocus(observed.GetRuntimeId(), editor.GetRuntimeId()) && WordCommentFocus.Editable(observed);
        }

        static void Dispatch(int role)
        {
            ModernWordSetup.Dispatch(scope, () => OwnedScratch() && WordCommentFocus.Editable(editor), () =>
            {
                if (!recoveryScope)
                {
                    Check(ModernWordSetup.Wait(() => OwnedScratch() && WordCommentFocus.Editable(editor), FixtureReady, () => Thread.Sleep(50), 60, 3), "The same comment has stable focus metadata before dispatch");
                    WaitIdle();
                }
            }, () => Check(Native.PostMessage(resident, 0x0312, new IntPtr(role), IntPtr.Zero), "Dispatch production resident shortcut"));
        }

        static void Case(string view, string action)
        {
            Check(Native.GetForegroundWindow() == owner, "Stop rather than refocusing after desktop interruption");
            CaseCore(view, action);
        }

        static void CaseCore(string view, string action)
        {
            if (action == "word" || action == "ctrlword" || action == "typing" || action == "undo" || action == "recovery")
            {
                Seed("Earlier untouched prasada");
                if (action == "ctrlword")
                {
                    // Deliver Ctrl to Word, then dispatch through the normal
                    // registered shortcut. The resident's injected-key filter
                    // remains intact; this isolates Word's Ctrl/focus effects.
                    var key = typeof(Native).GetMethod("Key", BindingFlags.Static | BindingFlags.NonPublic);
                    var ctrl = new[]
                    {
                        (Native.Input)key.Invoke(null, new object[] { (ushort)0xA2, false }),
                        (Native.Input)key.Invoke(null, new object[] { (ushort)0xA2, true })
                    };
                    Check(Native.GetForegroundWindow() == owner && Native.SendInput((uint)ctrl.Length, ctrl, Marshal.SizeOf(typeof(Native.Input))) == ctrl.Length, "Deliver Ctrl only to the owned scratch editor");
                }

                Dispatch(8);
                if (action == "recovery")
                {
                    WaitRecovery();
                    WaitIdle();
                    string afterRecovery = Text();
                    Check(afterRecovery == "Earlier untouched prasada" || afterRecovery == "Earlier untouched prasāda", "Recovery leaves only the original or verified converted text");
                    // No SetFocus or reseeding here: the refreshed helper must
                    // catch up while the same comment and caret remain active.
                    if (afterRecovery == "Earlier untouched prasada")
                        Dispatch(8);
                }

                WaitText("Earlier untouched prasāda");
                Check(object.Equals(Pattern().DocumentRange.FindText("prasāda", false, false).GetAttributeValue(TextPattern.IsItalicAttribute), false), "Modern comment retains its original plain formatting");
                Check(object.Equals(Pattern().GetSelection()[0].GetAttributeValue(TextPattern.IsItalicAttribute), false), "Typing format is restored after conversion");
                if (action == "undo" || action == "recovery")
                {
                    Check(Native.GetForegroundWindow() == owner && Native.Chord(0x5A, true), "Native undo in owned editor");
                    WaitText("Earlier untouched prasada");
                }

                if (action == "typing")
                {
                    Check(Native.GetForegroundWindow() == owner && Native.TypeText(" next Word"), "Type only into owned draft");
                    WaitText("Earlier untouched prasāda next Word");
                    Check(object.Equals(Pattern().GetSelection()[0].GetAttributeValue(TextPattern.IsItalicAttribute), false), "Continued English is nonitalic");
                }
            }
            else if (action == "letter")
            {
                Seed("Earlier untouched a");
                Dispatch(7);
                WaitText("Earlier untouched ā");
            }
            else if (action == "abbreviation")
            {
                Seed("Earlier untouched SP");
                Dispatch(8);
                WaitText("Earlier untouched Śrīla Prabhupāda");
            }
            else if (action == "selection")
            {
                Seed("Earlier untouched prasada and Krsna.");
                Pattern().DocumentRange.Select();
                Dispatch(1);
                WaitText("Earlier untouched prasāda and Kṛṣṇa.");
                Check(Pattern().GetSelection()[0].GetText(-1) == "Earlier untouched prasāda and Kṛṣṇa.", "Converted selection stays selected");
                Check(Native.GetForegroundWindow() == owner && Native.Chord(0x5A, true), "Undo selection conversion in owned editor");
                WaitText("Earlier untouched prasada and Krsna.");
            }

            Check(Body() == original, "Document body remains byte-for-byte unchanged");
            Check(WordCommentFocus.Editable(editor), "The scratch comment remains the live writable editor");
            bool ready = false;
            for (int poll = 0; poll < 40 && !ready; poll++)
            {
                Check(OwnedScratch() && WordCommentFocus.Editable(editor), "Retain the same comment while fixture metadata settles");
                bool modern;
                var observed = WordCommentFocus.Focused(owner, out modern);
                ready = modern && observed != null && EditGuard.SameFocus(observed.GetRuntimeId(), editor.GetRuntimeId()) && WordCommentFocus.Editable(observed);
                if (!ready)
                    Thread.Sleep(50);
            }

            Check(ready, "Fixture has fresh metadata for the same live comment");
            bool handled;
            Check(!WordSelection.Replace(owner, "not selected fixture text", "unused output", () => false, out handled) && !handled, "Modern editor bypasses native document replacement even before the deliberately false edit context");
            Check(Body() == original, "Native document-route probe cannot change the body");
            results.Add(new { view = view, action = action, editor = editor.Current.AutomationId, text = Text() });
            completedKeys.Add(view + ":" + action);
            Console.WriteLine("PASS " + view + " " + action);
        }

        static void NewComment()
        {
            object commands = Get(app, "CommandBars");
            try
            {
                editor = ModernWordSetup.Open(OwnedScratch, ActivateBody, WaitBody, () => Convert.ToBoolean(Call(commands, "GetEnabledMso", NewCommentCommand)), () => Call(commands, "ExecuteMso", NewCommentCommand), WaitEditor, value => WordCommentFocus.InComments(owner, value) && WordCommentFocus.Editable(value), () => Body() == original, stage =>
                {
                    setupStage = stage;
                    Console.WriteLine("SETUP " + stage);
                });
            }
            finally
            {
                Free(commands);
            }
        }

        static bool OwnedScratch()
        {
            IntPtr foreground = Native.GetForegroundWindow();
            uint process, foregroundProcess;
            Native.GetWindowThreadProcessId(owner, out process);
            Native.GetWindowThreadProcessId(foreground, out foregroundProcess);
            return WordApplication.SameWindow(owner, process, WordPasteOptions.Owner(foreground), foregroundProcess) && Convert.ToInt32(Get(documents, "Count")) == 1;
        }

        static object FailureFocus()
        {
            IntPtr foreground = Native.GetForegroundWindow();
            uint process;
            Native.GetWindowThreadProcessId(foreground, out process);
            var name = new StringBuilder(64);
            Native.GetClassName(foreground, name, name.Capacity);
            return new
            {
                window = foreground.ToInt64(),
                process = process,
                cls = name.ToString(),
                root_owner = WordPasteOptions.Owner(foreground).ToInt64(),
                expected_owner = owner.ToInt64()
            };
        }

        static void ActivateBody()
        {
            object window = null, pane = null, anchor = null;
            try
            {
                Check(OwnedScratch(), "Only the scratch window may activate a pane");
                window = Get(document, "ActiveWindow");
                Check(new IntPtr(Convert.ToInt64(Get(window, "Hwnd"))) == owner, "Body pane belongs to the owned scratch frame");
                pane = Get(window, "ActivePane");
                Call(pane, "Activate");
                Check(OwnedScratch(), "Stop if focus changes while activating the body");
                anchor = Call(document, "Range", 0, 7);
                Call(anchor, "Select");
            }
            finally
            {
                Free(anchor);
                Free(pane);
                Free(window);
            }
        }

        static bool WaitBody()
        {
            int stable = 0;
            for (int poll = 0; poll < 50; poll++)
            {
                if (!OwnedScratch())
                    return false;
                stable = WordPasteOptions.DocumentFocused(owner) && !WordCommentFocus.IsCommentFocus(owner) ? stable + 1 : 0;
                if (stable == 2)
                    return true;
                Thread.Sleep(20);
            }

            return false;
        }

        static string CommandLabel(string command)
        {
            object commands = Get(app, "CommandBars");
            try
            {
                return Convert.ToString(Call(commands, "GetLabelMso", command));
            }
            catch (TargetInvocationException)
            {
                return null;
            }
            catch (COMException)
            {
                return null;
            }
            finally
            {
                Free(commands);
            }
        }

        static void OpenPanel()
        {
            setupStage = "open-panel";
            panel.Open(panelAnchor);
            Check(Body() == original, "Opening the panel leaves body unchanged");
        }

        static void NewPanelComment()
        {
            setupStage = "panel-new-draft";
            panelHost = panel.NewDraft();
            editor = WaitEditor();
            Check(panel.Host(editor) == panelHost, "New draft belongs to the verified panel host");
            Check(Body() == original, "Panel draft creation leaves body unchanged");
        }

        static IEnumerable<string> AncestorCards()
        {
            var node = editor;
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            for (int depth = 0; depth < 18 && node != null; depth++, node = TreeWalker.RawViewWalker.GetParent(node))
            {
                Check(OwnedScratch() && node.Current.ProcessId == process, "Thread ancestry remains inside owned Word");
                if (node.Current.ClassName == "NetUIHWNDElement" && node.Current.NativeWindowHandle != 0)
                    yield break;
                if (node.Current.ControlType == ControlType.TreeItem)
                    yield return node.Current.AutomationId;
            }

            throw new InvalidOperationException("Thread ancestry did not reach its owned native host");
        }

        static AutomationElement PostOwnedComment()
        {
            setupStage = "post-comment";
            Check(OwnedScratch() && WordCommentFocus.Editable(editor), "Only the owned live comment may be posted");
            IntPtr host = panel.Host(editor);
            Check(host != IntPtr.Zero, "Comment has its verified native host before posting");
            string expected = ModernWordSetup.PostedCard(AncestorCards());
            var before = new HashSet<string>(panel.Cards(host).Select(card => card.Current.AutomationId).Where(id => id.StartsWith("cardRoot_", StringComparison.Ordinal)), StringComparer.Ordinal);
            Check(expected == null || before.Contains(expected), "Existing thread identity is present before posting");
            editor.SetFocus();
            Check(OwnedScratch() && WordCommentFocus.Editable(editor) && Native.Chord(0x0D, true), "Post only the prepared scratch comment");
            AutomationElement result = null;
            for (int poll = 0; poll < 100 && result == null; poll++)
            {
                Check(OwnedScratch(), "Scratch Word retains focus while its comment is posted");
                // A new draft can receive a different server ID on posting.
                // Select its one new card, or the exact known existing thread;
                // never guess from a temporary GUID or the first visible card.
                result = ModernWordSetup.FindFocused(panel.Cards(host), card => !card.Current.IsOffscreen && ModernWordSetup.PostedMatch(card.Current.AutomationId, expected, before), OwnedScratch);
                if (result == null)
                    Thread.Sleep(50);
            }

            Check(result != null, "The specific posted scratch thread is available");
            panelAnchor = result.Current.AutomationId;
            return result;
        }

        static bool InvokeOwnedAction(AutomationElement root, string[] labels)
        {
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            Check(Native.GetForegroundWindow() == owner && root != null && root.Current.ProcessId == (int)process, "Action root belongs to foreground scratch Word");
            AutomationElement action = null;
            var controls = root.FindAll(TreeScope.Element | TreeScope.Descendants, new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem)));
            foreach (AutomationElement control in controls)
                if (control.Current.ProcessId == (int)process && !control.Current.IsOffscreen && labels.Any(label => string.Equals(control.Current.Name.Trim(), label, StringComparison.OrdinalIgnoreCase)))
                {
                    Check(action == null, "Scratch action label must be unique");
                    action = control;
                }

            if (action == null)
                return false;
            object pattern = null;
            Check(Native.GetForegroundWindow() == owner && action.TryGetCurrentPattern(InvokePattern.Pattern, out pattern), "Owned action exposes InvokePattern");
            ((InvokePattern)pattern).Invoke();
            return true;
        }

        static void BeginPostedEdit()
        {
            setupStage = "posted-edit";
            Seed("Earlier untouched prasada");
            var card = PostOwnedComment();
            if (!InvokeOwnedAction(card, new[] { "Edit", "Edit comment" }))
            {
                Check(InvokeOwnedAction(card, new[] { "More thread actions", "More options", "More options for this comment" }), "Owned posted comment exposes an Edit or More action");
                AutomationElement menu = null;
                uint process;
                Native.GetWindowThreadProcessId(owner, out process);
                for (int poll = 0; poll < 40 && menu == null; poll++)
                {
                    Check(Native.GetForegroundWindow() == owner, "Scratch Word retains focus while its comment menu opens");
                    var node = AutomationElement.FocusedElement;
                    for (int depth = 0; depth < 8 && node != null && node.Current.ProcessId == (int)process; depth++, node = TreeWalker.RawViewWalker.GetParent(node))
                        if (node.Current.ControlType == ControlType.Menu)
                        {
                            menu = node;
                            break;
                        }

                    if (menu == null)
                        Thread.Sleep(50);
                }

                Check(menu != null && InvokeOwnedAction(menu, new[] { "Edit", "Edit comment" }), "Invoke Edit only in the owned posted-comment menu");
            }

            editor = WaitEditor();
            Check(editor.Current.AutomationId.StartsWith("postEditor_", StringComparison.Ordinal), "Fixture exercises the posted editor rather than a new draft");
        }

        static void PostedComment(string view)
        {
            BeginPostedEdit();
            foreach (string action in new[]
            {
                "word",
                "undo",
                "selection",
                "typing"
            }

            )
                Case(view, action);
        }

        static void Reply()
        {
            setupStage = "reply";
            var card = PostOwnedComment();
            AutomationElement reply = null;
            foreach (AutomationElement button in card.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
                if (WordCommentFocus.EditorId(button.Current.AutomationId))
                {
                    Check(reply == null, "Reply control is unique");
                    reply = button;
                }

            Check(reply != null, "Modern thread exposes a reply control");
            ((InvokePattern)reply.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            editor = WaitEditor();
        }

        static void SetupView(string view)
        {
            Check(OwnedScratch() && WordCommentFocus.Editable(editor) && Body() == original, "Setup view remains owned, writable and separate from the body");
            results.Add(new { view = view, action = "setup", editor = editor.Current.AutomationId });
        }

        static void SetupViews()
        {
            Check(!ModernWordSetup.Converts(scope), "View setup cannot dispatch conversions");
            NewComment();
            Seed("Scratch contextual setup");
            SetupView("contextual new");
            BeginPostedEdit();
            Seed("Scratch contextual edit");
            SetupView("contextual edit");
            Reply();
            Seed("Scratch contextual reply");
            SetupView("contextual reply");
            PostOwnedComment();
            OpenPanel();
            NewPanelComment();
            Seed("Scratch panel setup");
            SetupView("panel new");
            BeginPostedEdit();
            Seed("Scratch panel edit");
            SetupView("panel edit");
            Reply();
            Seed("Scratch panel reply");
            SetupView("panel reply");
            PostOwnedComment();
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == WordFocusMonitor.Argument)
                return WordFocusMonitor.HelperMain();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });
            if (args.Length == 1 && args[0] == "--self-test")
            {
                try
                {
                    ModernWordSetupTests.Run();
                    return 0;
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    return 1;
                }
            }

            if (args.Length == 2 && args[0] == "--scope-info")
            {
                try
                {
                    Console.WriteLine(new JavaScriptSerializer().Serialize(new { scope = ModernWordSetup.Scope(args[1]), needs_settlement = ModernWordSetup.Converts(args[1]), case_keys = ModernWordSetup.CaseKeys(args[1]) }));
                    return 0;
                }
                catch (ArgumentException error)
                {
                    Console.Error.WriteLine(error.Message);
                    return 2;
                }
            }

            if (args.Length != 7 || args[0] != "--desktop")
            {
                Console.Error.WriteLine("Modern Comments tests require explicit desktop authorization through the test wrapper.");
                return 2;
            }

            args = args.Skip(1).ToArray();
            scope = args[3] = ModernWordSetup.Scope(args[3]);
            ModernWordSetup.Converts(scope);
            recoveryScope = scope == "ContextualRecovery";
            Native.DpiAware();
            int[] modifiers =
            {
                0x10,
                0x11,
                0x12,
                0xA0,
                0xA1,
                0xA2,
                0xA3,
                0xA4,
                0xA5
            };
            if (!ModernWordSetup.Wait(() => true, () => modifiers.All(key => (Native.GetAsyncKeyState(key) & 0x8000) == 0), () => Thread.Sleep(50), 60, 3))
            {
                Console.Error.WriteLine("Modifier keys are still down; no Word window opened or input sent.");
                return 2;
            }

            string source = Path.GetFullPath(args[0]), scratch = Path.GetFullPath(args[1]);
            Check(scratch.StartsWith(Path.Combine(source, "out") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(scratch), "Fresh fixture under out required");
            Directory.CreateDirectory(scratch);
            int[] existing = Process.GetProcessesByName("WINWORD").Select(process => process.Id).ToArray();
            documents = null;
            bool owned = false;
            WordCommentFocus.Trace = message => Console.WriteLine("Fixture " + message);
            using (WordCommentFocus.Listen(message => Console.WriteLine("Fixture " + message)))
                try
                {
                    string profile = args.Length > 5 ? Path.GetFullPath(args[5]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyJaya");
                    resident = (IntPtr)typeof(LaunchRequests).GetMethod("FindWindow", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { null, LaunchRequests.Caption(profile) });
                    uint residentPid;
                    Native.GetWindowThreadProcessId(resident, out residentPid);
                    Check(residentPid == uint.Parse(args[2]), "Verified development resident is running");
                    testedResident = residentPid;
                    residentLog = Path.Combine(profile, "logs", "keyjaya.log");
                    app = Activator.CreateInstance(Type.GetTypeFromProgID("Word.Application", true));
                    documents = Get(app, "Documents");
                    Check(Convert.ToInt32(Get(documents, "Count")) == 0, "New Word application has no existing documents");
                    document = Call(documents, "Add");
                    object window = Get(document, "ActiveWindow");
                    owner = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                    uint pid;
                    Native.GetWindowThreadProcessId(owner, out pid);
                    Check(pid != 0 && !existing.Contains((int)pid), "Word process is new and exclusively owned");
                    owned = true;
                    panel = new ModernWordPanel(owner, OwnedScratch, CommandLabel);
                    Set(app, "DisplayAlerts", 0);
                    object view = Get(window, "View");
                    Set(view, "Type", 3);
                    Set(view, "SplitSpecial", 0);
                    Free(view);
                    Free(window);
                    object content = Get(document, "Content");
                    Set(content, "Text", "KeyJaya modern comments. This body must remain unchanged.");
                    Free(content);
                    original = Body();
                    Call(document, "SaveAs2", Path.Combine(scratch, "Modern-comment-test.docx"));
                    Set(app, "Visible", true);
                    Set(app, "WindowState", 1);
                    Call(app, "Activate");
                    Native.SetForegroundWindow(owner);
                    object anchor = Call(document, "Range", 0, 7);
                    Call(anchor, "Select");
                    Free(anchor);
                    if (scope == "SetupViews")
                    {
                        SetupViews();
                        Check(Body() == original, "View setup leaves document body unchanged");
                        File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = true, conversion_dispatched = false, queue_probe_used = false }));
                        Console.WriteLine("PASS: " + passed + " checks; six contextual/panel setup views; no resident conversion or settlement probe.");
                        return 0;
                    }

                    if (args.Length < 4 || !args[3].StartsWith("Panel", StringComparison.Ordinal))
                    {
                        if (args.Length > 3 && args[3] == "SetupOnly")
                        {
                            NewComment();
                            File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, setup_stage = setupStage, body_unchanged = Body() == original, conversion_dispatched = false }));
                            Console.WriteLine("PASS: " + passed + " checks; owned body focus, built-in New Comment command and writable editor; no conversion dispatched.");
                            return 0;
                        }

                        if (args.Length > 3 && args[3] == "ContextualRecovery")
                        {
                            // Verify silence before the editor exists, so no previous
                            // comment observation can hide a lost subscription.
                            VerifySilentHelper();
                            NewComment();
                            Case("contextual helper recovery", "recovery");
                            Check(ModernWordSetup.Complete(scope, completedKeys), "Every requested recovery case passed exactly once");
                            File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = Body() == original, resident_unchanged = true, silent_helper_recovered = true }));
                            Console.WriteLine("PASS: " + passed + " checks; alive silent helper recovered, same editor/caret, unchanged resident, conversion and native undo.");
                            return 0;
                        }

                        NewComment();
                        foreach (string action in new[]
                        {
                            "word",
                            "ctrlword",
                            "undo",
                            "typing",
                            "letter",
                            "abbreviation",
                            "selection"
                        }

                        )
                            Case("contextual new", action);
                        if (args.Length > 3 && args[3] == "ContextualNew")
                        {
                            Check(ModernWordSetup.Complete(scope, completedKeys), "Every requested contextual draft case passed exactly once");
                            File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = Body() == original }));
                            Console.WriteLine("PASS: " + passed + " checks; contextual draft including Word Ctrl/focus delivery; posted/reply cases excluded.");
                            return 0;
                        }

                        PostedComment("contextual posted edit");
                        Reply();
                        foreach (string action in new[]
                        {
                            "word",
                            "undo",
                            "typing",
                            "letter",
                            "abbreviation",
                            "selection"
                        }

                        )
                            Case("contextual reply", action);
                        editor.SetFocus();
                        Check(Native.Chord(0x0D, true), "Post owned scratch reply");
                    }
                    else
                    {
                        // Panel-only modes still need a real posted thread to
                        // identify their pane; no stale/null editor is reused.
                        NewComment();
                        Seed("Scratch panel parent");
                        PostOwnedComment();
                    }

                    if (args[3] == "ContextualAll")
                    {
                        Check(ModernWordSetup.Complete(scope, completedKeys), "Every requested contextual case passed exactly once");
                        Check(Body() == original, "All contextual actions retain the original body");
                        File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = true, comments_panel_excluded = true }));
                        Console.WriteLine("PASS: " + passed + " checks; all contextual new/edit/reply conversions and undo; separate Comments panel excluded.");
                        return 0;
                    }

                    if (args.Length > 4 && args[4] == "manual")
                    {
                        File.WriteAllText(Path.Combine(scratch, "panel-ready.json"), new JavaScriptSerializer().Serialize(new { window = owner.ToInt64(), process = pid }));
                        Console.WriteLine("PANEL_READY window=" + owner + " process=" + pid + " fixture=" + scratch);
                        for (int wait = 0; wait < 900 && !File.Exists(Path.Combine(scratch, "panel-opened")); wait++)
                            Thread.Sleep(100);
                        Check(File.Exists(Path.Combine(scratch, "panel-opened")), "Owned fixture Comments panel must be opened");
                        Native.SetForegroundWindow(owner);
                    }
                    else
                        OpenPanel();
                    NewPanelComment();
                    if (args.Length < 4 || args[3] != "PanelReply")
                    {
                        foreach (string action in new[]
                        {
                            "word",
                            "undo",
                            "typing",
                            "letter",
                            "abbreviation",
                            "selection"
                        }

                        )
                            Case("panel new", action);
                        PostedComment("panel posted edit");
                    }
                    else
                        Seed("Scratch parent stays unchanged");
                    Reply();
                    foreach (string action in new[]
                    {
                        "word",
                        "undo",
                        "typing",
                        "letter",
                        "abbreviation",
                        "selection"
                    }

                    )
                        Case("panel reply", action);
                    Check(ModernWordSetup.Complete(scope, completedKeys), "Every requested comment view/action passed exactly once");
                    File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = Body() == original }));
                    Console.WriteLine("PASS: " + passed + " checks; " + results.Count + " Modern Comments cases; new/reply, word/letter/selection, undo and English continuation.");
                    return 0;
                }
                catch (Exception error)
                {
                    object foreground = FailureFocus();
                    File.WriteAllText(Path.Combine(scratch, "failure.json"), new JavaScriptSerializer().Serialize(new { passed = passed, setup_stage = setupStage, cases = results, foreground = foreground, original_body = original, current_body = document == null ? null : Body(), error = error.ToString() }));
                    Console.WriteLine(error.ToString());
                    return 1;
                }
                finally
                {
                    try
                    {
                        CleanupFault();
                    }
                    finally
                    {
                        try
                        {
                            if (owned && app != null && Convert.ToInt32(Get(documents, "Count")) == 1)
                                Call(app, "Quit", 0);
                            else if (document != null)
                                Call(document, "Close", 0);
                        }
                        finally
                        {
                            Free(document);
                            Free(documents);
                            Free(app);
                        }
                    }
                }
        }
    }
}
