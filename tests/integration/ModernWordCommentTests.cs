// Opt-in integration checks in a new, exclusively owned local Word document.
// Uses the running, hash-verified development resident's normal hotkey dispatch.
// Input is limited to the owned Modern Comments editor; no manuscript is opened.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
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
        static object app, document;
        static string original;
        static readonly List<object> results = new List<object>();
        [StructLayout(LayoutKind.Sequential)]
        struct ClickPoint
        {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(ClickPoint point);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
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
            return EmbeddedWebViewFocus.Focused(owner);
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
                var focus = Focus();
                if (focus != null && WordCommentFocus.InComments(owner, focus) && WordCommentFocus.Editable(focus))
                    return focus;
                Thread.Sleep(50);
            }

            var last = (AutomationElement)typeof(WordCommentFocus).GetField("last", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var chain = new List<object>();
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            for (int depth = 0; depth < 16 && last != null && last.Current.ProcessId == (int)process; depth++, last = TreeWalker.RawViewWalker.GetParent(last))
            {
                var nativeClass = new System.Text.StringBuilder(64);
                Native.GetClassName(new IntPtr(last.Current.NativeWindowHandle), nativeClass, nativeClass.Capacity);
                chain.Add(new { id = last.Current.AutomationId, cls = last.Current.ClassName, native = nativeClass.ToString(), focus = last.Current.HasKeyboardFocus });
            }

            throw new InvalidOperationException("No focused writable Modern Comments editor: " + new JavaScriptSerializer().Serialize(chain));
        }

        static void WaitText(string expected)
        {
            for (int poll = 0; poll < 160; poll++)
            {
                if (Native.GetForegroundWindow() != owner)
                    throw new InvalidOperationException("Owned scratch window lost focus");
                if (Text() == expected)
                {
                    WaitIdle();
                    return;
                }

                Thread.Sleep(50);
            }

            throw new InvalidOperationException("Expected: " + expected + "; observed: " + Text());
        }

        static void Seed(string text)
        {
            Native.SetForegroundWindow(owner);
            editor.SetFocus();
            editor = WaitEditor();
            ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text);
            WaitText(text);
            var end = Pattern().DocumentRange;
            end.MoveEndpointByRange(TextPatternRangeEndpoint.Start, end, TextPatternRangeEndpoint.End);
            end.Select();
            if (object.Equals(Pattern().GetSelection()[0].GetAttributeValue(TextPattern.IsItalicAttribute), true))
                Check(Native.Chord(0x49, true), "Initialize owned fixture typing style");
            Thread.Sleep(400);
        }

        static void Dispatch(int role)
        {
            Check(Native.GetForegroundWindow() == owner && WordCommentFocus.Editable(editor), "Owned modern editor must retain focus before dispatch");
            Check(Native.PostMessage(resident, 0x0312, new IntPtr(role), IntPtr.Zero), "Dispatch production resident shortcut");
        }

        static void Case(string view, string action)
        {
            for (int attempt = 0;; attempt++)
                try
                {
                    CaseCore(view, action);
                    return;
                }
                catch (Exception)
                {
                    if (attempt >= 2 || Native.GetForegroundWindow() == owner)
                        throw;
                    Console.WriteLine("Fixture focus interrupted; retry owned case " + view + " " + action);
                    WaitIdle();
                    Native.SetForegroundWindow(owner);
                    editor.SetFocus();
                    Thread.Sleep(400);
                }
        }

        static void CaseCore(string view, string action)
        {
            if (action == "word" || action == "typing" || action == "undo")
            {
                Seed("Earlier untouched prasada");
                Dispatch(8);
                WaitText("Earlier untouched prasāda");
                Check(object.Equals(Pattern().DocumentRange.FindText("prasāda", false, false).GetAttributeValue(TextPattern.IsItalicAttribute), false), "Modern comment retains its original plain formatting");
                Check(object.Equals(Pattern().GetSelection()[0].GetAttributeValue(TextPattern.IsItalicAttribute), false), "Typing format is restored after conversion");
                if (action == "undo")
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
            Check(WordApplication.ForWindow(owner) == null, "Modern editor cannot bind to native body Selection");
            results.Add(new { view = view, action = action, editor = editor.Current.AutomationId, text = Text() });
            Console.WriteLine("PASS " + view + " " + action);
        }

        static void NewComment()
        {
            var makeKey = typeof(Native).GetMethod("Key", BindingFlags.Static | BindingFlags.NonPublic);
            var keys = new[]
            {
                new ushort[]
                {
                    0x11,
                    0
                },
                new ushort[]
                {
                    0x12,
                    0
                },
                new ushort[]
                {
                    0x4D,
                    0
                },
                new ushort[]
                {
                    0x4D,
                    1
                },
                new ushort[]
                {
                    0x12,
                    1
                },
                new ushort[]
                {
                    0x11,
                    1
                }
            }.Select(key => (Native.Input)makeKey.Invoke(null, new object[] { key[0], key[1] != 0 })).ToArray();
            Check(Native.GetForegroundWindow() == owner && Native.SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Native.Input))) == keys.Length, "Open modern draft in owned Word using Ctrl+Alt+M");
            editor = WaitEditor();
        }

        static void OpenPanel()
        {
            Native.SetForegroundWindow(owner);
            var button = AutomationElement.FromHandle(owner).FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.NameProperty, "Comments")));
            Check(button != null && !button.Current.IsOffscreen, "Owned Word exposes Comments view control");
            var bounds = button.Current.BoundingRectangle;
            var point = new ClickPoint
            {
                X = (int)(bounds.X + bounds.Width / 2),
                Y = (int)(bounds.Y + bounds.Height / 2)
            };
            Check(Native.GetForegroundWindow() == owner && WordPasteOptions.Owner(WindowFromPoint(point)) == owner, "Comments view control belongs to the owned Word window");
            Check(SetCursorPos(point.X, point.Y), "Point only at owned Comments view control");
            mouse_event(2, 0, 0, 0, new UIntPtr(Native.OwnInput));
            mouse_event(4, 0, 0, 0, new UIntPtr(Native.OwnInput));
            Thread.Sleep(400);
            Check(Body() == original, "Opening the view must not change fixture body");
        }

        static void NewPanelComment()
        {
            var node = (AutomationElement)typeof(WordCommentFocus).GetField("last", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            for (int depth = 0; depth < 16 && node != null; depth++, node = TreeWalker.RawViewWalker.GetParent(node))
                if (node.Current.ClassName == "NetUIHWNDElement" && node.Current.NativeWindowHandle != 0)
                    break;
            Check(node != null, "Actual Comments panel accessibility host is available");
            var buttons = node.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            AutomationElement add = null;
            foreach (AutomationElement button in buttons)
                if (button.Current.Name.Trim() == "New" || button.Current.Name.Trim().StartsWith("New comment", StringComparison.OrdinalIgnoreCase))
                {
                    Check(add == null, "Panel New button is unique");
                    add = button;
                }

            Check(add != null, "Actual Comments panel exposes New button");
            object invoke;
            if (add.TryGetCurrentPattern(InvokePattern.Pattern, out invoke))
                ((InvokePattern)invoke).Invoke();
            else
            {
                var bounds = add.Current.BoundingRectangle;
                var point = new ClickPoint
                {
                    X = (int)(bounds.X + bounds.Width / 2),
                    Y = (int)(bounds.Y + bounds.Height / 2)
                };
                Check(Native.GetForegroundWindow() == owner && WordPasteOptions.Owner(WindowFromPoint(point)) == owner, "Panel New control belongs to owned Word");
                Check(SetCursorPos(point.X, point.Y), "Point at owned Panel New control");
                mouse_event(2, 0, 0, 0, new UIntPtr(Native.OwnInput));
                mouse_event(4, 0, 0, 0, new UIntPtr(Native.OwnInput));
            }

            editor = WaitEditor();
        }

        static void Reply()
        {
            editor.SetFocus();
            Check(Native.Chord(0x0D, true), "Post only the owned local scratch comment");
            AutomationElement card = null;
            for (int poll = 0; poll < 100; poll++)
            {
                var focus = Focus();
                if (focus != null && focus.Current.ControlType == ControlType.TreeItem && WordCommentFocus.CardId(focus.Current.AutomationId) && !focus.Current.AutomationId.StartsWith("draft", StringComparison.Ordinal))
                {
                    card = focus;
                    break;
                }

                Thread.Sleep(50);
            }

            Check(card != null, "Posted scratch comment owns modern focus");
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

        [STAThread]
        public static int Main(string[] args)
        {
            Native.DpiAware();
            string source = Path.GetFullPath(args[0]), scratch = Path.GetFullPath(args[1]);
            Check(scratch.StartsWith(Path.Combine(source, "out") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(scratch), "Fresh fixture under out required");
            Directory.CreateDirectory(scratch);
            int[] existing = Process.GetProcessesByName("WINWORD").Select(process => process.Id).ToArray();
            object documents = null;
            bool owned = false;
            using (WordCommentFocus.Listen())
                try
                {
                    string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KeyJaya");
                    resident = (IntPtr)typeof(LaunchRequests).GetMethod("FindWindow", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { null, LaunchRequests.Caption(profile) });
                    uint residentPid;
                    Native.GetWindowThreadProcessId(resident, out residentPid);
                    Check(residentPid == uint.Parse(args[2]), "Verified development resident is running");
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
                    if (args.Length < 4 || !args[3].StartsWith("Panel", StringComparison.Ordinal))
                    {
                        NewComment();
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
                            Case("contextual new", action);
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
                    File.WriteAllText(Path.Combine(scratch, "results.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, body_unchanged = Body() == original }));
                    Console.WriteLine("PASS: " + passed + " checks; " + results.Count + " Modern Comments cases; new/reply, word/letter/selection, undo and English continuation.");
                    return 0;
                }
                catch (Exception error)
                {
                    File.WriteAllText(Path.Combine(scratch, "failure.json"), new JavaScriptSerializer().Serialize(new { passed = passed, cases = results, original_body = original, current_body = document == null ? null : Body(), error = error.ToString() }));
                    Console.WriteLine(error.ToString());
                    return 1;
                }
                finally
                {
                    if (owned && app != null)
                        Call(app, "Quit", 0);
                    Free(document);
                    Free(documents);
                    Free(app);
                }
        }
    }
}
