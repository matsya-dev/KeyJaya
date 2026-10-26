// Modern Comments focus events expose the real draft editor when FocusedElement
// returns Word's legacy document provider on the same _WwG HWND. Cached events
// are evidence only: every use rechecks live focus, ancestry and window ownership.
// Fetch properties together and resolve the observed editor before asking Word
// for its often-stale global focus provider. No ownership/text proof survives a call.
using System;
using System.Text;
using System.Threading;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class WordCommentFocus
    {
        static readonly object gate = new object ();
        static AutomationElement last;
        public static Action<string> Trace;
        internal static bool EditorId(string id)
        {
            return id != null && id.StartsWith("cardEditor_", StringComparison.Ordinal) && id.Length > "cardEditor_".Length;
        }

        internal static bool CardId(string id)
        {
            return id != null && (id.StartsWith("draftCardRoot_", StringComparison.Ordinal) && id.Length > "draftCardRoot_".Length || id.StartsWith("cardRoot_", StringComparison.Ordinal) && id.Length > "cardRoot_".Length);
        }

        internal static bool HostClass(string name)
        {
            return name == "_WwG" || name == "NetUIHWND";
        }

        static void Changed(object sender, AutomationFocusChangedEventArgs args)
        {
            lock (gate)
                last = sender as AutomationElement;
        }

        public static IDisposable Listen()
        {
            return new Listener();
        }

        sealed class Listener : IDisposable
        {
            readonly ManualResetEvent stop = new ManualResetEvent(false), ready = new ManualResetEvent(false);
            readonly Thread thread;
            int disposed;
            public Listener()
            {
                thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "KeyJaya Word comment focus"
                };
                thread.SetApartmentState(ApartmentState.MTA);
                thread.Start();
                ready.WaitOne(1000);
            }

            void Run()
            {
                AutomationFocusChangedEventHandler handler = Changed;
                bool registered = false;
                try
                {
                    Automation.AddAutomationFocusChangedEventHandler(handler);
                    registered = true;
                    ready.Set();
                    stop.WaitOne();
                }
                catch (Exception error)
                {
                    if (Trace != null)
                        Trace("Word comment focus unavailable: " + error.GetType().Name);
                }
                finally
                {
                    ready.Set();
                    if (registered)
                        try
                        {
                            Automation.RemoveAutomationFocusChangedEventHandler(handler);
                        }
                        catch (Exception)
                        {
                        }

                    lock (gate)
                        last = null;
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref disposed, 1) != 0)
                    return;
                stop.Set();
                if (thread.Join(1000))
                {
                    stop.Dispose();
                    ready.Dispose();
                }
            }
        }

        public static bool InComments(IntPtr owner, AutomationElement element)
        {
            return InComments(owner, element, false);
        }

        // A single provider round trip fills each node's properties. Parent
        // retrieval uses the same request rather than reading Current repeatedly.
        static CacheRequest Properties()
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
                AutomationElement.ControlTypeProperty,
                AutomationElement.ClassNameProperty,
                AutomationElement.NativeWindowHandleProperty,
                AutomationElement.HasKeyboardFocusProperty,
                AutomationElement.IsEnabledProperty,
                AutomationElement.IsPasswordProperty
            }

            )
                request.Add(property);
            return request;
        }

        internal sealed class Node
        {
            public int Process;
            public string Id, Class;
            public ControlType Type;
            public IntPtr Handle;
        }

        internal static IntPtr CommentHost<T>(T element, int process, Func<T, Node> read, Func<T, T> parent)
            where T : class
        {
            bool card = false;
            for (int count = 0; count < 18 && element != null; count++, element = parent(element))
            {
                Node node = read(element);
                if (node.Process != process)
                    return IntPtr.Zero;
                card |= CardId(node.Id) && node.Type == ControlType.TreeItem;
                if (card && node.Class == "NetUIHWNDElement")
                    return node.Handle;
            }

            return IntPtr.Zero;
        }

        static bool InComments(IntPtr owner, AutomationElement element, bool requireFocus)
        {
            try
            {
                if (element == null || owner == IntPtr.Zero || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()) || !WordPasteOptions.IsWord(owner))
                    return false;
                uint process;
                Native.GetWindowThreadProcessId(owner, out process);
                if (process == 0)
                    return false;
                var request = Properties();
                var current = element.GetUpdatedCache(request);
                if (requireFocus && !current.Cached.HasKeyboardFocus)
                    return false;
                IntPtr handle = CommentHost(current, (int)process, node =>
                {
                    var info = node.Cached;
                    return new Node
                    {
                        Process = info.ProcessId,
                        Id = info.AutomationId,
                        Type = info.ControlType,
                        Class = info.ClassName,
                        Handle = new IntPtr(info.NativeWindowHandle)
                    };
                }, node => TreeWalker.RawViewWalker.GetParent(node, request));
                if (handle == IntPtr.Zero)
                    return false;
                uint nativeProcess;
                Native.GetWindowThreadProcessId(handle, out nativeProcess);
                var name = new StringBuilder(64);
                Native.GetClassName(handle, name, name.Capacity);
                return nativeProcess == process && HostClass(name.ToString()) && WordPasteOptions.Owner(handle) == owner && owner == WordPasteOptions.Owner(Native.GetForegroundWindow());
            }
            catch (Exception)
            {
            }

            return false;
        }

        // Kept independent of UIA so tests can prove a successful event-based
        // resolution never calls the slow global-focus fallback.
        internal static T Resolve<T>(T observed, Func<T, bool> comment, Func<T> normal, Func<T, bool> hasFocus, out bool modern)
            where T : class
        {
            modern = false;
            if (observed != null && comment(observed))
            {
                modern = true;
                return observed;
            }

            T fallback = normal();
            if (fallback == null || !hasFocus(fallback))
                return null;
            modern = comment(fallback);
            return fallback;
        }

        public static AutomationElement Focused(IntPtr owner)
        {
            bool modern;
            return Focused(owner, out modern);
        }

        public static AutomationElement Focused(IntPtr owner, out bool modern)
        {
            modern = false;
            try
            {
                AutomationElement candidate;
                lock (gate)
                    candidate = last;
                // A legacy document provider reporting lost focus must not let
                // its old body caret authorize an edit in a different surface.
                return Resolve(candidate, element => InComments(owner, element, true), () => AutomationElement.FocusedElement, element => element.Current.HasKeyboardFocus, out modern);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsCommentFocus(IntPtr owner)
        {
            try
            {
                if (!WordPasteOptions.IsWord(owner))
                    return false;
                bool modern;
                return Focused(owner, out modern) != null && modern;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool Editable(AutomationElement element)
        {
            if (element == null)
                return false;
            var info = element.GetUpdatedCache(Properties()).Cached;
            if (!info.HasKeyboardFocus || !info.IsEnabled || info.IsPassword || info.ControlType != ControlType.Edit || !EditorId(info.AutomationId))
                return false;
            object provider;
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                return false;
            var selected = ((TextPattern)provider).GetSelection();
            return selected.Length == 1 && object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), false);
        }
    }
}
