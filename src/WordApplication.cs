// KeyJaya: Bind Word automation to the requested editor window, not an arbitrary
// registered Word instance. This module reads window metadata only; selection,
// document identity, text, formatting and undo remain WordSelection's responsibility.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyJaya
{
    internal static class WordApplication
    {
        public static Action<string> Trace;
        delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr window, WindowVisitor visitor, IntPtr parameter);
        [DllImport("oleacc.dll")]
        static extern int AccessibleObjectFromWindow(IntPtr window, uint objectId, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object value);
        static object Get(object value, string property)
        {
            return value.GetType().InvokeMember(property, BindingFlags.GetProperty, null, value, null);
        }

        static void Release(object value)
        {
            try
            {
                if (value != null && Marshal.IsComObject(value))
                    Marshal.ReleaseComObject(value);
            }
            catch (Exception)
            {
            // A disconnected server must not prevent cleanup or safe fallback.
            }
        }

        internal static bool SameWindow(IntPtr expectedRoot, uint expectedProcess, IntPtr actualRoot, uint actualProcess)
        {
            return expectedRoot != IntPtr.Zero && expectedProcess != 0 && expectedRoot == actualRoot && expectedProcess == actualProcess;
        }

        // An acquired reference is either transferred to the caller or released
        // once on every rejection/error. No COM object is cached across apartments.
        static object TryLookup(Func<object> lookup, Func<object, bool> matches, Action<object> release)
        {
            object candidate = null;
            try
            {
                candidate = lookup();
                if (candidate == null || !matches(candidate))
                    return null;
                object accepted = candidate;
                candidate = null;
                return accepted;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (candidate != null)
                    release(candidate);
            }
        }

        internal static object Resolve(Func<object> windowApplication, Func<object> registeredApplication, Func<object, bool> matches, Action<object> release)
        {
            return TryLookup(windowApplication, matches, release) ?? TryLookup(registeredApplication, matches, release);
        }

        static object FromPane(IntPtr pane)
        {
            object window = null;
            try
            {
                var dispatch = new Guid("00020400-0000-0000-C000-000000000046");
                // Microsoft's OBJID_NATIVEOM contract exposes Word.Window on _WwG.
                // https://learn.microsoft.com/windows/win32/api/oleacc/nf-oleacc-accessibleobjectfromwindow
                int result = AccessibleObjectFromWindow(pane, 0xFFFFFFF0u, ref dispatch, out window);
                return result == 0 && window != null ? Get(window, "Application") : null;
            }
            finally
            {
                Release(window);
            }
        }

        static object FromWindow(IntPtr root, uint process)
        {
            object application = null;
            int visited = 0;
            EnumChildWindows(root, (child, unused) =>
            {
                if (++visited > 1024)
                    return false;
                var name = new StringBuilder(64);
                Native.GetClassName(child, name, name.Capacity);
                uint childProcess;
                Native.GetWindowThreadProcessId(child, out childProcess);
                if (name.ToString() != "_WwG" || childProcess != process)
                    return true;
                try
                {
                    application = FromPane(child);
                }
                catch (Exception)
                {
                // Split/hidden panes may not expose the object model. Continue
                // only inside this Word window; never enumerate documents.
                }

                return application == null;
            }, IntPtr.Zero);
            return application;
        }

        internal static object ForWindow(IntPtr owner)
        {
            IntPtr root = WordPasteOptions.Owner(owner);
            if (root == IntPtr.Zero || !WordPasteOptions.IsWord(root))
                return null;
            uint process;
            Native.GetWindowThreadProcessId(root, out process);
            if (process == 0)
                return null;
            // Prefer the actual foreground frame when it shares this root owner.
            IntPtr frame = Native.GetAncestor(Native.GetForegroundWindow(), 2);
            if (frame == IntPtr.Zero || WordPasteOptions.Owner(frame) != root)
                frame = root;
            return Resolve(() => FromWindow(frame, process), () => Marshal.GetActiveObject("Word.Application"), application =>
            {
                object window = null;
                try
                {
                    window = Get(application, "ActiveWindow");
                    IntPtr actual = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                    uint actualProcess, currentProcess;
                    Native.GetWindowThreadProcessId(actual, out actualProcess);
                    Native.GetWindowThreadProcessId(root, out currentProcess);
                    bool match = currentProcess == process && SameWindow(root, process, WordPasteOptions.Owner(actual), actualProcess);
                    if (Trace != null)
                        Trace("Word binding matched=" + match + " requested-pid=" + process + " application-pid=" + actualProcess);
                    return match;
                }
                finally
                {
                    Release(window);
                }
            }, Release);
        }
    }
}
