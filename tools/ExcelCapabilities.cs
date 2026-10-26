// Read-only Excel accessibility metadata. Never reads cell text/values, sends
// input, invokes a UI action, opens a workbook or changes foreground focus.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class ExcelCapabilities
    {
        delegate bool Visitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumWindows(Visitor visit, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr window, Visitor visit, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);
        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")]
        static extern bool GetGUIThreadInfo(uint thread, ref ThreadInfo info);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        static extern IntPtr SelectionMessage(IntPtr window, uint message, ref int start, ref int end, uint flags, uint timeout, out UIntPtr result);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        static extern IntPtr LengthMessage(IntPtr window, uint message, IntPtr first, IntPtr second, uint flags, uint timeout, out UIntPtr result);
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct ThreadInfo
        {
            public uint Size, Flags;
            public IntPtr Active, Focus, Capture, Menu, Move, Caret;
            public Rect Bounds;
        }

        static string Class(IntPtr window)
        {
            var value = new StringBuilder(80);
            GetClassName(window, value, value.Capacity);
            return value.ToString();
        }

        static object Describe(AutomationElement element)
        {
            try
            {
                var request = new CacheRequest
                {
                    TreeScope = TreeScope.Element,
                    TreeFilter = Automation.RawViewCondition
                };
                foreach (var property in new[]
                {
                    AutomationElement.ProcessIdProperty,
                    AutomationElement.AutomationIdProperty,
                    AutomationElement.ClassNameProperty,
                    AutomationElement.ControlTypeProperty,
                    AutomationElement.NativeWindowHandleProperty,
                    AutomationElement.HasKeyboardFocusProperty,
                    AutomationElement.IsOffscreenProperty,
                    AutomationElement.IsTextPatternAvailableProperty,
                    AutomationElement.IsValuePatternAvailableProperty
                }

                )
                    request.Add(property);
                var watch = Stopwatch.StartNew();
                var current = element.GetUpdatedCache(request);
                var info = current.Cached;
                return new
                {
                    process = info.ProcessId,
                    id = info.AutomationId,
                    provider_class = info.ClassName,
                    type = info.ControlType.ProgrammaticName,
                    handle = info.NativeWindowHandle,
                    focused = info.HasKeyboardFocus,
                    offscreen = info.IsOffscreen,
                    text_pattern = current.GetCachedPropertyValue(AutomationElement.IsTextPatternAvailableProperty),
                    value_pattern = current.GetCachedPropertyValue(AutomationElement.IsValuePatternAvailableProperty),
                    metadata_ms = watch.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception error)
            {
                return new
                {
                    unavailable = error.GetType().Name
                };
            }
        }

        static object Inspect()
        {
            var roots = new List<object>();
            IntPtr foreground = GetForegroundWindow();
            int windows = 0;
            EnumWindows((root, unused) =>
            {
                if (++windows > 1024)
                    return false;
                if (Class(root) != "XLMAIN")
                    return true;
                uint pid;
                uint thread = GetWindowThreadProcessId(root, out pid);
                var state = new ThreadInfo
                {
                    Size = (uint)Marshal.SizeOf(typeof(ThreadInfo))
                };
                bool available = GetGUIThreadInfo(thread, ref state);
                var editors = new List<object>();
                int visited = 0;
                EnumChildWindows(root, (child, ignored) =>
                {
                    if (++visited > 512 || editors.Count >= 8)
                        return false;
                    string name = Class(child);
                    if (name != "EXCEL6" && name != "EXCEL<")
                        return true;
                    uint childPid;
                    GetWindowThreadProcessId(child, out childPid);
                    if (childPid == pid && GetAncestor(child, 2) == root)
                    {
                        int start = int.MinValue, end = int.MinValue;
                        UIntPtr selectionResult, lengthResult;
                        bool selectionAnswered = SelectionMessage(child, 0xB0, ref start, ref end, 0x23, 100, out selectionResult) != IntPtr.Zero;
                        bool lengthAnswered = LengthMessage(child, 0xE, IntPtr.Zero, IntPtr.Zero, 0x23, 100, out lengthResult) != IntPtr.Zero;
                        // Plausible numbers alone do not prove a custom control
                        // implements the standard Edit protocol (unsupported
                        // Office windows can return arbitrary values or zeros).
                        bool plausible = selectionAnswered && lengthAnswered && start >= 0 && end >= start && (ulong)end <= lengthResult.ToUInt64();
                        editors.Add(new { native_class = name, visible = IsWindowVisible(child), was_thread_focus = available && state.Focus == child, native_selection_plausible = plausible, native_selection_start = plausible ? (int? )start : null, native_selection_end = plausible ? (int? )end : null, native_length_answered = lengthAnswered, native_length = lengthResult.ToUInt64(), metadata = Describe(AutomationElement.FromHandle(child)) });
                    }

                    return true;
                }, IntPtr.Zero);
                var children = new List<object>();
                try
                {
                    var nodes = AutomationElement.FromHandle(root).FindAll(TreeScope.Children, Condition.TrueCondition);
                    for (int n = 0; n < Math.Min(nodes.Count, 32); n++)
                        children.Add(Describe(nodes[n]));
                }
                catch (Exception error)
                {
                    children.Add(new { unavailable = error.GetType().Name });
                }

                roots.Add(new { process = pid, foreground = foreground == root, visible = IsWindowVisible(root), thread_focus_class = available ? Class(state.Focus) : null, editors = editors, immediate_children = children });
                return roots.Count < 4;
            }, IntPtr.Zero);
            return new
            {
                read_only = true,
                cell_text_read = false,
                desktop_input_sent = false,
                foreground_unchanged = foreground == GetForegroundWindow(),
                excel_windows = roots
            };
        }

        static int Main()
        {
            var task = Task.Factory.StartNew<object>(() => Inspect());
            object result = task.Wait(3000) ? task.Result : new
            {
                read_only = true,
                deadline_exceeded = true,
                cell_text_read = false,
                desktop_input_sent = false
            };
            Console.WriteLine(new JavaScriptSerializer().Serialize(result));
            return 0;
        }
    }
}
