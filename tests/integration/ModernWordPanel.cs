// Native fixture discovery only; no production build input or cached edit permission.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Automation;

namespace KeyJaya
{
    internal sealed class ModernWordPanel
    {
        readonly IntPtr owner;
        readonly Func<bool> current;
        readonly Func<string, string> label;
        string anchor;
        delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr window, WindowVisitor visitor, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        sealed class Root
        {
            internal IntPtr Window;
            internal AutomationElement Element;
        }

        sealed class PanelAction
        {
            internal IntPtr Host;
            internal AutomationElement Element;
        }

        internal ModernWordPanel(IntPtr owner, Func<bool> current, Func<string, string> label)
        {
            this.owner = owner;
            this.current = current;
            this.label = label;
        }

        static void Require(bool value, string message)
        {
            if (!value)
                throw new InvalidOperationException(message);
        }

        bool OwnedWindow(IntPtr window)
        {
            uint process, candidate;
            Native.GetWindowThreadProcessId(owner, out process);
            Native.GetWindowThreadProcessId(window, out candidate);
            return window != IntPtr.Zero && WordApplication.SameWindow(owner, process, WordPasteOptions.Owner(window), candidate);
        }

        IEnumerable<Root> Roots(bool frame)
        {
            Require(current(), "Only the scratch frame may discover native hosts");
            var windows = new List<IntPtr>();
            if (frame)
                windows.Add(owner);
            bool exceeded = false;
            WindowVisitor visit = (window, ignored) =>
            {
                var name = new StringBuilder(64);
                Native.GetClassName(window, name, name.Capacity);
                if (WordCommentFocus.HostClass(name.ToString()) && OwnedWindow(window) && IsWindowVisible(window))
                {
                    if (windows.Count >= 64)
                    {
                        exceeded = true;
                        return false;
                    }

                    windows.Add(window);
                }

                return true;
            };
            EnumChildWindows(owner, visit, IntPtr.Zero);
            Require(!exceeded && current(), "Native fixture host discovery is bounded and owned");
            foreach (IntPtr window in windows)
            {
                Require(current() && OwnedWindow(window), "Native fixture host changed before UIA discovery");
                yield return new Root
                {
                    Window = window,
                    Element = AutomationElement.FromHandle(window)
                };
            }
        }

        internal IntPtr Host(AutomationElement node)
        {
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            for (int depth = 0; depth < 18 && node != null; depth++, node = TreeWalker.RawViewWalker.GetParent(node))
            {
                var info = node.Current;
                if (!current() || info.ProcessId != process)
                    return IntPtr.Zero;
                if (info.ClassName == "NetUIHWNDElement" && info.NativeWindowHandle != 0)
                {
                    IntPtr window = new IntPtr(info.NativeWindowHandle);
                    var name = new StringBuilder(64);
                    Native.GetClassName(window, name, name.Capacity);
                    return OwnedWindow(window) && WordCommentFocus.HostClass(name.ToString()) ? window : IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }

        bool OwnedControl(AutomationElement node)
        {
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            for (int depth = 0; depth < 24 && node != null; depth++, node = TreeWalker.RawViewWalker.GetParent(node))
            {
                var info = node.Current;
                if (!current() || info.ProcessId != process)
                    return false;
                if (info.NativeWindowHandle != 0)
                    return OwnedWindow(new IntPtr(info.NativeWindowHandle));
            }

            return false;
        }

        IEnumerable<AutomationElement> Controls(AutomationElement root)
        {
            var nodes = root.FindAll(TreeScope.Element | TreeScope.Descendants, new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.SplitButton), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem)));
            Require(nodes.Count <= 2048, "Owned action enumeration exceeds its bound; count=" + nodes.Count);
            Require(current(), "Scratch focus changed during owned action enumeration");
            foreach (AutomationElement node in nodes)
                yield return node;
        }

        ModernWordSetup.ActionInfo Describe(AutomationElement node, bool toggle)
        {
            var info = node.Current;
            object provider;
            return new ModernWordSetup.ActionInfo
            {
                Identity = string.Join(",", node.GetRuntimeId().Select(value => value.ToString(CultureInfo.InvariantCulture))),
                Label = info.Name,
                Owned = OwnedControl(node),
                Visible = !info.IsOffscreen && !info.BoundingRectangle.IsEmpty && info.BoundingRectangle.Width > 0 && info.BoundingRectangle.Height > 0,
                Enabled = info.IsEnabled,
                Supported = node.TryGetCurrentPattern(InvokePattern.Pattern, out provider) || toggle && node.TryGetCurrentPattern(TogglePattern.Pattern, out provider)
            };
        }

        string[] Labels(string command, params string[] fallback)
        {
            var values = new List<string>(fallback);
            string localized = label(command);
            if (!string.IsNullOrEmpty(localized))
                values.Add(localized);
            return values.ToArray();
        }

        void Invoke(AutomationElement node, string[] labels, bool allowToggle)
        {
            string identity = Describe(node, allowToggle).Identity;
            ModernWordSetup.Act(current, () =>
            {
                var fresh = Describe(node, allowToggle);
                return fresh.Identity == identity && ModernWordSetup.Matches(fresh, labels);
            }, () =>
            {
                object provider;
                if (node.TryGetCurrentPattern(InvokePattern.Pattern, out provider))
                    ((InvokePattern)provider).Invoke();
                else
                {
                    Require(allowToggle && node.TryGetCurrentPattern(TogglePattern.Pattern, out provider), "Owned action retains a supported pattern");
                    var toggle = (TogglePattern)provider;
                    Require(toggle.Current.ToggleState == ToggleState.Off, "Never turn off an already-enabled Comments view");
                    toggle.Toggle();
                }
            });
        }

        IEnumerable<AutomationElement> ViewControls(string[] labels)
        {
            foreach (Root root in Roots(true))
                foreach (AutomationElement node in Controls(root.Element))
                    if (ModernWordSetup.CaptionMatches(node.Current.Name, labels))
                        yield return node;
        }

        internal IEnumerable<AutomationElement> Cards(IntPtr host)
        {
            Require(current() && OwnedWindow(host), "Card discovery remains in the scratch host");
            var cards = AutomationElement.FromHandle(host).FindAll(TreeScope.Element | TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem));
            Require(cards.Count <= 256, "Card discovery is bounded");
            foreach (AutomationElement card in cards)
                if (WordCommentFocus.CardId(card.Current.AutomationId) && OwnedControl(card))
                    yield return card;
        }

        bool HasAnchor(IntPtr host)
        {
            return ModernWordSetup.FindFocused(Cards(host), card => ModernWordSetup.SamePostedThread(card.Current.AutomationId, anchor) && !card.Current.IsOffscreen, current) != null;
        }

        IEnumerable<PanelAction> NewActions(string[] labels)
        {
            // Do not enumerate a ribbon's command tree before proving that its
            // native host contains the specific posted scratch thread.
            foreach (AutomationElement node in ModernWordSetup.Anchored(Roots(false), root => HasAnchor(root.Window), root => Controls(root.Element), current))
                if (ModernWordSetup.CaptionMatches(node.Current.Name, labels) && ModernWordSetup.Matches(Describe(node, false), labels))
                {
                    IntPtr host = Host(node);
                    if (host != IntPtr.Zero && HasAnchor(host))
                        yield return new PanelAction
                        {
                            Host = host,
                            Element = node
                        };
                }
        }

        PanelAction FindNew(string[] labels)
        {
            Require(!string.IsNullOrEmpty(anchor), "A posted scratch thread anchors panel discovery");
            return ModernWordSetup.FindAction(NewActions(labels), value => Describe(value.Element, false), labels, current);
        }

        void TraceUnavailable(string[] labels)
        {
            // Failure evidence only: inspect the owned scratch frame before its
            // finally block closes Word. Never invoke another action to diagnose it.
            try
            {
                foreach (Root root in Roots(true))
                {
                    Require(current(), "Scratch focus changed before panel diagnostics");
                    var cards = root.Element.FindAll(TreeScope.Element | TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem));
                    Require(cards.Count <= 256, "Panel diagnostic card enumeration is bounded");
                    var ids = cards.Cast<AutomationElement>().Where(OwnedControl).Select(card => new { id = card.Current.AutomationId, visible = !card.Current.IsOffscreen }).ToArray();
                    var actions = Controls(root.Element).Where(node => ModernWordSetup.CaptionMatches(node.Current.Name, labels)).Select(node => new { info = Describe(node, true), id = node.Current.AutomationId, role = node.Current.ControlType.ProgrammaticName, host = Host(node).ToInt64() }).ToArray();
                    Require(current() && OwnedWindow(root.Window), "Panel diagnostics remain in the scratch frame");
                    Console.WriteLine("PANEL unavailable " + new JavaScriptSerializer().Serialize(new { window = root.Window.ToInt64(), cls = root.Element.Current.ClassName, anchor = anchor, cards = ids, actions = actions }));
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("PANEL diagnostics stopped: " + error.GetType().Name + ": " + error.Message);
            }
        }

        internal void Open(string postedAnchor)
        {
            anchor = postedAnchor;
            string[] newLabels = Labels("ReviewNewComment", "New", "New comment");
            string[] viewLabels = Labels("ReviewShowComments", "Comments", "Show comments");
            try
            {
                ModernWordSetup.OpenView(current, () => FindNew(newLabels) != null, () =>
                {
                    var button = ModernWordSetup.FindAction(ViewControls(viewLabels), value => Describe(value, true), viewLabels, current);
                    Require(button != null, "Scratch Word exposes a unique supported Comments action");
                    Invoke(button, viewLabels, true);
                }, () => Thread.Sleep(50));
            }
            catch
            {
                TraceUnavailable(newLabels.Concat(viewLabels).ToArray());
                throw;
            }
        }

        internal IntPtr NewDraft()
        {
            string[] labels = Labels("ReviewNewComment", "New", "New comment");
            var action = FindNew(labels);
            Require(action != null, "Fresh panel discovery identifies the owned New action");
            Require(HasAnchor(action.Host), "Panel anchor is revalidated before creating a draft");
            Invoke(action.Element, labels, false);
            return action.Host;
        }
    }
}
