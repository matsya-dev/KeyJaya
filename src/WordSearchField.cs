// Word's docked Navigation query is a native Rich Edit, separate from Selection.
// Recognize its owned native path before consulting UIA; never fall back to a
// stale document provider when the native query field has keyboard focus.
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class WordSearchField
    {
        internal sealed class Node
        {
            internal string Class;
            internal int Process, Id;
        }

        static readonly string[] classes =
        {
            "RICHEDIT60W",
            "NetUICtrlNotifySink",
            "NetUIHWND",
            "NUIPane",
            "MsoWorkPane",
            "MsoCommandBar",
            "MsoCommandBarDock",
            "OpusApp"
        };
        internal static bool Path(IntPtr field, IntPtr owner, int process, Func<IntPtr, Node> read, Func<IntPtr, IntPtr> parent)
        {
            if (field == IntPtr.Zero || owner == IntPtr.Zero || process <= 0)
                return false;
            for (int depth = 0; depth < classes.Length; depth++)
            {
                if (field == IntPtr.Zero)
                    return false;
                var node = read(field);
                if (node == null || node.Process != process || !string.Equals(node.Class, classes[depth], StringComparison.OrdinalIgnoreCase) || depth == 0 && node.Id != 1)
                    return false;
                if (depth == classes.Length - 1)
                    return field == owner;
                if (field == owner)
                    return false;
                field = parent(field);
            }

            return false;
        }

        internal static bool Writable(int style)
        {
            // Disabled/password/read-only/multiline controls are not this query.
            return (style & (0x08000000 | 0x20 | 0x800 | 0x4)) == 0;
        }

        internal static bool Editor(string name, ControlType type, bool focused, bool enabled, bool password, bool sameWindow, bool sameProcess)
        {
            return name == "NetUITextbox" && type == ControlType.Edit && focused && enabled && !password && sameWindow && sameProcess;
        }

        internal static bool Output(string text)
        {
            if (text == null)
                return false;
            foreach (char c in text)
                // Unicode line/paragraph separators are not Char.IsControl.
                // Ordinary Unicode spaces remain valid literal query text.
                if (char.IsControl(c) || c == '\u2028' || c == '\u2029')
                    return false;
            return true;
        }

        internal static T Resolve<T>(bool query, Func<T> field, Func<T> document)
        {
            return query ? field() : document();
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll")]
        static extern int GetDlgCtrlID(IntPtr window);
        static string Class(IntPtr window)
        {
            var name = new StringBuilder(128);
            Native.GetClassName(window, name, name.Capacity);
            return name.ToString();
        }

        static IntPtr NativeField(IntPtr owner)
        {
            if (owner == IntPtr.Zero || owner != Native.GetForegroundWindow())
                return IntPtr.Zero;
            var identity = Native.FocusIdentity();
            IntPtr field = identity == null ? IntPtr.Zero : new IntPtr((uint)identity[4] | ((long)identity[5] << 32));
            // Body/comment providers must not pay for an ancestry/UIA scan.
            if (field == IntPtr.Zero || !string.Equals(Class(field), classes[0], StringComparison.OrdinalIgnoreCase) || !WordPasteOptions.IsWord(owner))
                return IntPtr.Zero;
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            bool match = Path(field, owner, (int)process, window =>
            {
                uint current;
                Native.GetWindowThreadProcessId(window, out current);
                return new Node
                {
                    Class = Class(window),
                    Process = (int)current,
                    Id = GetDlgCtrlID(window)
                };
            }, GetParent);
            return match && owner == Native.GetForegroundWindow() && EditGuard.SameFocus(identity, Native.FocusIdentity()) ? field : IntPtr.Zero;
        }

        // Structural recognition also blocks Word's document bridge when this
        // field is read-only or its UIA provider is temporarily unavailable.
        internal static bool NativeFocused(IntPtr owner)
        {
            return NativeField(owner) != IntPtr.Zero;
        }

        static bool ValidEditor(IntPtr owner, IntPtr field, AutomationElement element)
        {
            if (element == null || !Writable(Native.EditStyle(field, -16)))
                return false;
            var request = new CacheRequest
            {
                TreeScope = TreeScope.Element
            };
            foreach (var property in new[]
            {
                AutomationElement.ProcessIdProperty,
                AutomationElement.ClassNameProperty,
                AutomationElement.ControlTypeProperty,
                AutomationElement.HasKeyboardFocusProperty,
                AutomationElement.IsEnabledProperty,
                AutomationElement.IsPasswordProperty,
                AutomationElement.NativeWindowHandleProperty
            }

            )
                request.Add(property);
            var info = element.GetUpdatedCache(request).Cached;
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            return Editor(info.ClassName, info.ControlType, info.HasKeyboardFocus, info.IsEnabled, info.IsPassword, new IntPtr(info.NativeWindowHandle) == field, info.ProcessId == (int)process) && NativeField(owner) == field;
        }

        internal static AutomationElement FocusedElement(IntPtr owner)
        {
            try
            {
                IntPtr field = NativeField(owner);
                if (field == IntPtr.Zero)
                    return null;
                var element = AutomationElement.FocusedElement;
                return ValidEditor(owner, field, element) ? element : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static bool Focused(IntPtr owner, AutomationElement element)
        {
            try
            {
                IntPtr field = NativeField(owner);
                return field != IntPtr.Zero && ValidEditor(owner, field, element);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
