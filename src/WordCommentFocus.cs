// Modern Comments focus events expose the real draft editor when FocusedElement
// returns Word's legacy document provider on the same _WwG HWND. Cached events
// are evidence only: every use rechecks live focus, ancestry and window ownership.
// Fetch properties together and resolve the observed editor before asking Word
// for its often-stale global focus provider. No ownership/text proof survives a call.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace KeyJaya
{
    internal static class WordCommentFocus
    {
        static readonly FocusObservation<FocusLookup<AutomationElement>> observed = new FocusObservation<FocusLookup<AutomationElement>>();
        static WordFocusMonitor monitor;
        public static Action<string> Trace;
        static void Detail(string message)
        {
            try
            {
                var trace = Trace;
                if (trace != null)
                    trace(message);
            }
            catch (Exception)
            {
            }
        }

        internal static bool EditorId(string id)
        {
            // Posted comments use postEditor_, while new drafts/replies use
            // cardEditor_. Both still need live focus, card ancestry, the native
            // Word host and a writable TextPattern before they can be edited.
            return id != null && (id.StartsWith("cardEditor_", StringComparison.Ordinal) && id.Length > "cardEditor_".Length || id.StartsWith("postEditor_", StringComparison.Ordinal) && id.Length > "postEditor_".Length);
        }

        internal static bool CardId(string id)
        {
            return id != null && (id.StartsWith("draftCardRoot_", StringComparison.Ordinal) && id.Length > "draftCardRoot_".Length || id.StartsWith("cardRoot_", StringComparison.Ordinal) && id.Length > "cardRoot_".Length);
        }

        internal static bool HostClass(string name)
        {
            return name == "_WwG" || name == "NetUIHWND";
        }

        public static IDisposable Listen(Action<string> log = null)
        {
            var listening = new WordFocusMonitor((session, notice) =>
            {
                observed.Publish(session, notice == null ? null : new FocusLookup<AutomationElement>(() =>
                {
                    var element = FromNotice(notice);
                    Detail("Word focus locator resolved=" + (element != null));
                    return element;
                }, notice, element =>
                {
                    var current = observed.Read();
                    if (!session.Active || current == null || !ReferenceEquals(current.Identity, notice) || !InComments(new IntPtr(notice.Owner), element, true))
                        return;
                    current = observed.Read();
                    // A bounded caller can time out before this MTA read succeeds.
                    // Do not replace a healthy helper after fresh live proof, and
                    // never let an old completion cancel a new session's recovery.
                    if (session.Active && current != null && ReferenceEquals(current.Identity, notice))
                        Recovery(new IntPtr(notice.Owner), true);
                }), (left, right) => SameNotice((WordFocusNotice)left.Identity, (WordFocusNotice)right.Identity));
                FocusLookup<AutomationElement>.Prepare(observed);
            }, log);
            Interlocked.Exchange(ref monitor, listening);
            return listening;
        }

        internal static bool SameNotice(WordFocusNotice left, WordFocusNotice right)
        {
            if (left == null || right == null || left.Owner != right.Owner || left.Host != right.Host || left.Process != right.Process || left.Editor != right.Editor || !EditGuard.SameFocus(left.Runtime, right.Runtime))
                return false;
            if (left.Bounds == null || right.Bounds == null)
                return left.Bounds == right.Bounds;
            if (left.Bounds.Length != right.Bounds.Length)
                return false;
            for (int i = 0; i < left.Bounds.Length; i++)
                if (left.Bounds[i] != right.Bounds[i])
                    return false;
            return true;
        }

        internal static WordFocusNotice Describe(IntPtr owner, AutomationElement element)
        {
            if (!WordPasteOptions.IsWord(owner) || element == null)
                return null;
            var info = element.GetUpdatedCache(Properties()).Cached;
            if (info.ControlType != ControlType.Edit || !EditorId(info.AutomationId) || !info.IsEnabled || info.IsPassword)
                return null;
            // Observe the early focus event; authorize only after live focus
            // settles at edit time. Otherwise the sole real editor can be lost.
            IntPtr host = CommentWindow(owner, element, false);
            if (host == IntPtr.Zero)
                return null;
            var bounds = element.Current.BoundingRectangle;
            return new WordFocusNotice
            {
                Owner = owner.ToInt64(),
                Host = host.ToInt64(),
                Process = info.ProcessId,
                Editor = info.AutomationId,
                Runtime = element.GetRuntimeId(),
                Bounds = bounds.IsEmpty ? null : new[]
                {
                    bounds.Left,
                    bounds.Top,
                    bounds.Width,
                    bounds.Height
                }
            };
        }

        // Only the disposable helper calls this catch-up probe. It reads focus
        // metadata, never text or pointer position, and cannot move the caret.
        // The native caret is a location hint, not proof of an editable target.
        internal static WordFocusNotice CurrentNotice()
        {
            try
            {
                IntPtr owner = WordPasteOptions.Owner(Native.GetForegroundWindow());
                if (!WordPasteOptions.IsWord(owner))
                    return null;
                var identity = Native.FocusIdentity();
                var editor = CatchUp(() => AutomationElement.FocusedElement, () => CaretElement(owner), element => InComments(owner, element, true), element => TreeWalker.RawViewWalker.GetParent(element), () => FocusedHostEditor(owner, identity));
                if (identity == null || !EditGuard.SameFocus(identity, Native.FocusIdentity()))
                    return null;
                return Describe(owner, editor);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static T CatchUp<T>(Func<T> focused, Func<T> caret, Func<T, bool> comment, Func<T, T> parent, Func<T> hosted = null)
            where T : class
        {
            try
            {
                T direct = focused();
                if (direct != null && comment(direct))
                    return direct;
            }
            catch (Exception)
            {
            }

            // UIA hit testing can return text inside the editor. Each candidate
            // still needs live focus and the full existing comment/host proof.
            try
            {
                T node = caret();
                for (int depth = 0; depth < 10 && node != null; depth++, node = parent(node))
                    if (comment(node))
                        return node;
            }
            catch (Exception)
            {
            }

            return hosted == null ? null : hosted();
        }

        internal static T UniqueFocused<T>(IEnumerable<T> candidates, Func<T, bool> valid, Func<bool> current)
            where T : class
        {
            T found = null;
            int count = 0;
            if (!current())
                return null;
            foreach (T candidate in candidates)
            {
                if (++count > 64 || !current())
                    return null;
                if (!valid(candidate))
                    continue;
                if (found != null)
                    return null;
                found = candidate;
            }

            return found != null && current() && valid(found) && current() ? found : null;
        }

        // The native focus host can expose only Word's legacy body provider,
        // while the owned frame exposes the live modern comment. This metadata
        // catch-up stays in the disposable MTA helper and retains live focus.
        static AutomationElement FocusedHostEditor(IntPtr owner, int[] identity)
        {
            if (identity == null || identity.Length != 6 || !EditGuard.SameFocus(identity, Native.FocusIdentity()))
                return null;
            IntPtr host = new IntPtr((long)(uint)identity[4] | ((long)identity[5] << 32));
            uint process, hostProcess;
            Native.GetWindowThreadProcessId(owner, out process);
            Native.GetWindowThreadProcessId(host, out hostProcess);
            var name = new StringBuilder(64);
            Native.GetClassName(host, name, name.Capacity);
            if (host == IntPtr.Zero || process == 0 || hostProcess != process || WordPasteOptions.Owner(host) != owner || !HostClass(name.ToString()))
                return null;
            Func<IntPtr, AutomationElement> find = window =>
            {
                var candidates = AutomationElement.FromHandle(window).FindAll(TreeScope.Element | TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, (int)process), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit), new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true), new PropertyCondition(AutomationElement.IsEnabledProperty, true), new PropertyCondition(AutomationElement.IsPasswordProperty, false)));
                return UniqueFocused(Elements(candidates), element => InComments(owner, element, true), () => owner == Native.GetForegroundWindow() && EditGuard.SameFocus(identity, Native.FocusIdentity()));
            };
            return Locate(() => find(host), () => find(owner));
        }

        static IEnumerable<AutomationElement> Elements(AutomationElementCollection candidates)
        {
            foreach (AutomationElement candidate in candidates)
                yield return candidate;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CaretInfo
        {
            public int Size, Flags;
            public IntPtr Active, Focus, Capture, Menu, Move, Caret;
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CaretPoint
        {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        static extern bool GetGUIThreadInfo(uint thread, ref CaretInfo info);
        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr window, ref CaretPoint point);
        static AutomationElement CaretElement(IntPtr owner)
        {
            uint process, caretProcess;
            uint thread = Native.GetWindowThreadProcessId(owner, out process);
            var info = new CaretInfo
            {
                Size = Marshal.SizeOf(typeof(CaretInfo))
            };
            if (thread == 0 || !GetGUIThreadInfo(thread, ref info) || info.Caret == IntPtr.Zero || info.Bottom <= info.Top || WordPasteOptions.Owner(info.Caret) != owner)
                return null;
            Native.GetWindowThreadProcessId(info.Caret, out caretProcess);
            var name = new StringBuilder(64);
            Native.GetClassName(info.Caret, name, name.Capacity);
            if (caretProcess != process || !HostClass(name.ToString()))
                return null;
            var point = new CaretPoint
            {
                X = info.Left,
                Y = checked((int)((long)info.Top + ((long)info.Bottom - info.Top) / 2))
            };
            return ClientToScreen(info.Caret, ref point) ? AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y)) : null;
        }

        internal static T Locate<T>(Func<T> point, Func<T> tree, Func<T> frame = null)
            where T : class
        {
            // Word's legacy HWND root can be unavailable, or omit the modern
            // comment fragment entirely. Do not construct it before trying the
            // independently verified point locator for that fragment.
            try
            {
                T candidate = point();
                if (candidate != null)
                    return candidate;
            }
            catch (Exception error)
            {
                Detail("Word focus primary lookup failed: " + error.GetType().Name);
            }

            // A modern reply can be present in the frame's provider tree but
            // omitted by the HWND provider at its point or native host.
            return frame == null ? tree() : Locate(tree, frame);
        }

        static AutomationElement FromNotice(WordFocusNotice notice)
        {
            if (notice == null)
                return null;
            try
            {
                IntPtr owner = new IntPtr(notice.Owner), host = new IntPtr(notice.Host);
                if (owner != WordPasteOptions.Owner(Native.GetForegroundWindow()) || !WordPasteOptions.IsWord(owner))
                    return null;
                uint process, hostProcess;
                Native.GetWindowThreadProcessId(owner, out process);
                Native.GetWindowThreadProcessId(host, out hostProcess);
                var name = new StringBuilder(64);
                Native.GetClassName(host, name, name.Capacity);
                if (process != (uint)notice.Process || hostProcess != process || !HostClass(name.ToString()) || WordPasteOptions.Owner(host) != owner)
                    return null;
                Func<IntPtr, AutomationElement> find = window =>
                {
                    var candidates = AutomationElement.FromHandle(window).FindAll(TreeScope.Element | TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.AutomationIdProperty, notice.Editor), new PropertyCondition(AutomationElement.ProcessIdProperty, notice.Process), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit), new PropertyCondition(AutomationElement.IsPasswordProperty, false)));
                    return UniqueFocused(Elements(candidates), element => EditGuard.SameFocus(notice.Runtime, element.GetRuntimeId()) && CommentWindow(owner, element, true) == host, () => owner == WordPasteOptions.Owner(Native.GetForegroundWindow()));
                };
                return Locate(() =>
                {
                    bool sameRuntime = false;
                    if (notice.Bounds != null && notice.Bounds.Length == 4 && notice.Bounds[2] > 0 && notice.Bounds[3] > 0)
                    {
                        var hit = AutomationElement.FromPoint(new System.Windows.Point(notice.Bounds[0] + notice.Bounds[2] / 2, notice.Bounds[1] + notice.Bounds[3] / 2));
                        for (int n = 0; n < 10 && hit != null; n++, hit = TreeWalker.RawViewWalker.GetParent(hit))
                            if (EditGuard.SameFocus(notice.Runtime, hit.GetRuntimeId()))
                            {
                                sameRuntime = true;
                                if (CommentWindow(owner, hit, true) == host)
                                    return hit;
                            }
                    }

                    Detail("Word focus point lookup unavailable; same runtime=" + sameRuntime);
                    return null;
                }, () => find(host), () => find(owner));
            }
            catch (Exception error)
            {
                Detail("Word focus locator lookup failed: " + error.GetType().Name);
                return null;
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

        internal static IntPtr CommentHost<T>(T element, int process, Func<T, Node> read, Func<T, T> parent, bool requireEditor = false)
            where T : class
        {
            bool card = false;
            for (int count = 0; count < 18 && element != null; count++, element = parent(element))
            {
                Node node = read(element);
                if (node.Process != process)
                    return IntPtr.Zero;
                // Body/ribbon focus is overwhelmingly more common. Reject it
                // after one property batch instead of scanning its ancestry on
                // every word, selection, letter and replay guard.
                if (count == 0 && requireEditor && (node.Type != ControlType.Edit || !EditorId(node.Id)))
                    return IntPtr.Zero;
                card |= CardId(node.Id) && node.Type == ControlType.TreeItem;
                if (card && node.Class == "NetUIHWNDElement")
                    return node.Handle;
            }

            return IntPtr.Zero;
        }

        static bool InComments(IntPtr owner, AutomationElement element, bool requireFocus)
        {
            return CommentWindow(owner, element, requireFocus) != IntPtr.Zero;
        }

        static IntPtr CommentWindow(IntPtr owner, AutomationElement element, bool requireFocus)
        {
            try
            {
                if (element == null || owner == IntPtr.Zero || owner != WordPasteOptions.Owner(Native.GetForegroundWindow()) || !WordPasteOptions.IsWord(owner))
                    return IntPtr.Zero;
                uint process;
                Native.GetWindowThreadProcessId(owner, out process);
                if (process == 0)
                    return IntPtr.Zero;
                var request = Properties();
                var current = element.GetUpdatedCache(request);
                if (!current.Cached.IsEnabled || current.Cached.IsPassword || requireFocus && !current.Cached.HasKeyboardFocus)
                    return IntPtr.Zero;
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
                }, node => TreeWalker.RawViewWalker.GetParent(node, request), true);
                if (handle == IntPtr.Zero)
                    return IntPtr.Zero;
                uint nativeProcess;
                Native.GetWindowThreadProcessId(handle, out nativeProcess);
                var name = new StringBuilder(64);
                Native.GetClassName(handle, name, name.Capacity);
                return nativeProcess == process && HostClass(name.ToString()) && WordPasteOptions.Owner(handle) == owner && owner == WordPasteOptions.Owner(Native.GetForegroundWindow()) ? handle : IntPtr.Zero;
            }
            catch (Exception)
            {
            }

            return IntPtr.Zero;
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
                AutomationElement candidate = FocusLookup<AutomationElement>.ReadCurrent(observed);
                if (candidate != null && !InComments(owner, candidate, true))
                {
                    FocusLookup<AutomationElement>.Invalidate(observed, candidate);
                    candidate = FocusLookup<AutomationElement>.ReadCurrent(observed);
                }

                // A legacy document provider reporting lost focus must not let
                // its old body caret authorize an edit in a different surface.
                var result = Resolve(candidate, element => InComments(owner, element, true), () => AutomationElement.FocusedElement, element => element.Current.HasKeyboardFocus, out modern);
                Recovery(owner, result != null);
                return result;
            }
            catch (Exception)
            {
                Recovery(owner, false);
                return null;
            }
        }

        static void Recovery(IntPtr owner, bool available)
        {
            var active = Interlocked.CompareExchange(ref monitor, null, null);
            if (active == null)
                return;
            if (available)
                active.CancelRecovery();
            else if (owner == WordPasteOptions.Owner(Native.GetForegroundWindow()) && WordPasteOptions.IsWord(owner))
                active.RequestRecovery(FocusLookup<AutomationElement>.Pending(observed));
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
