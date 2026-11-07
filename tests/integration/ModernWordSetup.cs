// Test setup policy, independent of Word/UIA so interrupted setup can be tested
// without a desktop. A selected range alone does not prove typing focus.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace KeyJaya
{
    internal static class ModernWordSetup
    {
        internal static string Scope(string scope)
        {
            foreach (string known in new[]
            {
                "SetupOnly",
                "SetupViews",
                "All",
                "Panel",
                "PanelReply",
                "ContextualNew",
                "ContextualAll",
                "ContextualRecovery"
            }

            )
                if (string.Equals(scope, known, StringComparison.OrdinalIgnoreCase))
                    return known;
            throw new ArgumentException("Unknown comment test scope", "scope");
        }

        internal static bool Converts(string scope)
        {
            switch (Scope(scope))
            {
                case "SetupOnly":
                case "SetupViews":
                    return false;
                case "All":
                case "Panel":
                case "PanelReply":
                case "ContextualNew":
                case "ContextualAll":
                case "ContextualRecovery":
                    return true;
                default:
                    throw new ArgumentException("Unknown comment test scope", "scope");
            }
        }

        internal static string[] CaseKeys(string scope)
        {
            scope = Scope(scope);
            var result = new List<string>();
            Action<string, string[]> add = (view, actions) =>
            {
                foreach (string action in actions)
                    result.Add(view + ":" + action);
            };
            var normal = new[]
            {
                "word",
                "undo",
                "typing",
                "letter",
                "abbreviation",
                "selection"
            };
            var posted = new[]
            {
                "word",
                "undo",
                "selection",
                "typing"
            };
            if (scope == "ContextualRecovery")
                add("contextual helper recovery", new[] { "recovery" });
            if (scope == "All" || scope == "ContextualAll" || scope == "ContextualNew")
            {
                add("contextual new", new[] { "word", "ctrlword", "undo", "typing", "letter", "abbreviation", "selection" });
                if (scope != "ContextualNew")
                {
                    add("contextual posted edit", posted);
                    add("contextual reply", normal);
                }
            }

            if (scope == "All" || scope == "Panel" || scope == "PanelReply")
            {
                if (scope != "PanelReply")
                {
                    add("panel new", normal);
                    add("panel posted edit", posted);
                }

                add("panel reply", normal);
            }

            return result.ToArray();
        }

        internal static bool Complete(string scope, IEnumerable<string> observed)
        {
            var expected = new HashSet<string>(CaseKeys(scope), StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string key in observed)
                if (!expected.Contains(key) || !seen.Add(key))
                    return false;
            return seen.SetEquals(expected);
        }

        internal static bool Wait(Func<bool> current, Func<bool> ready, Action pause, int polls, int stableSamples)
        {
            Require(polls > 0 && stableSamples > 0 && stableSamples <= polls, "Invalid fixture wait bounds");
            int stable = 0;
            for (int poll = 0; poll < polls; poll++)
            {
                Require(current(), "Scratch focus changed before readiness inspection");
                bool available = ready();
                Require(current(), "Scratch focus changed during readiness inspection");
                stable = available ? stable + 1 : 0;
                if (stable == stableSamples)
                    return true;
                if (poll + 1 < polls)
                    pause();
            }

            return false;
        }

        internal static void Seed(string text, bool recovery, Func<bool> current, Action<string> replace, Action<string> type, Action<string> waitText, Action collapse, Func<bool> ready, Action pause)
        {
            Require(current(), "Scratch editor changed before seeding");
            replace(recovery ? text : "");
            Require(current(), "Scratch editor changed while clearing/seeding");
            if (!recovery)
            {
                type(text);
                Require(current(), "Scratch editor changed while typing prepared input");
            }

            waitText(text);
            Require(current(), "Scratch editor changed while verifying seed text");
            collapse();
            Require(current(), "Scratch editor changed while placing its caret");
            if (!recovery)
                Require(Wait(current, ready, pause, 60, 3), "Comment focus metadata did not settle before conversion dispatch");
        }

        internal static void SetupUndoBoundary(bool recovery, Func<bool> current, Action boundary, Func<bool> unchanged, Action pause)
        {
            // Ordinary setup typing must not become part of the conversion's
            // native Word undo unit. Recovery must retain its no-input fault.
            if (recovery)
                return;
            Require(current(), "Scratch ownership changed before setup undo boundary");
            boundary();
            Require(Wait(current, unchanged, pause, 20, 2), "Setup undo boundary changed scratch text or caret");
        }

        internal sealed class ActionInfo
        {
            public string Identity, Label;
            public bool Owned, Visible, Enabled, Supported;
        }

        internal static string Label(string value)
        {
            if (value == null || value.Length > 256)
                return "";
            value = value.Replace("&", "").Replace("\u200e", "").Replace("\u200f", "").Trim();
            int tab = value.IndexOf('\t');
            if (tab >= 0)
                value = value.Substring(0, tab);
            value = value.TrimEnd('.', '\u2026').Trim();
            value = Regex.Replace(value, @"\s*\((?:[0-9]+|(?:Ctrl|Alt|Shift)(?:\+[^()]+)+)\)\s*$", "", RegexOptions.IgnoreCase);
            value = value.TrimEnd('.', '\u2026').Trim();
            return Regex.Replace(value.Normalize(), @"\s+", " ");
        }

        internal static bool Matches(ActionInfo info, string[] labels)
        {
            if (info == null || !info.Owned || !info.Visible || !info.Enabled || !info.Supported || string.IsNullOrEmpty(info.Identity))
                return false;
            return CaptionMatches(info.Label, labels);
        }

        internal static bool CaptionMatches(string caption, string[] labels)
        {
            string actual = Label(caption);
            if (actual.Length == 0)
                return false;
            foreach (string label in labels)
                if (string.Equals(actual, Label(label), StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        internal static IEnumerable<TControl> Anchored<TRegion, TControl>(IEnumerable<TRegion> regions, Func<TRegion, bool> hasAnchor, Func<TRegion, IEnumerable<TControl>> controls, Func<bool> current)
        {
            Require(current(), "Scratch focus changed before anchored discovery");
            int count = 0;
            foreach (TRegion region in regions)
            {
                Require(++count <= 64 && current(), "Anchored region discovery exceeded its bound or lost focus");
                bool eligible = hasAnchor(region);
                Require(current(), "Scratch focus changed during anchor inspection");
                if (!eligible)
                    continue;
                foreach (TControl control in controls(region))
                {
                    Require(current(), "Scratch focus changed during anchored control discovery");
                    yield return control;
                }
            }
        }

        internal static T FindAction<T>(IEnumerable<T> controls, Func<T, ActionInfo> describe, string[] labels, Func<bool> current)
            where T : class
        {
            Require(current(), "Scratch focus changed before action discovery");
            T result = null;
            string selectedIdentity = null;
            var identities = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (T control in controls)
            {
                Require(++count <= 2048 && current(), "Scratch action discovery exceeded its bound or lost focus");
                ActionInfo info = describe(control);
                Require(current(), "Scratch focus changed during action inspection");
                if (!Matches(info, labels) || !identities.Add(info.Identity))
                    continue;
                Require(result == null, "Scratch action is ambiguous");
                result = control;
                selectedIdentity = info.Identity;
            }

            Require(current(), "Scratch focus changed after action discovery");
            if (result == null)
                return null;
            ActionInfo final = describe(result);
            return Matches(final, labels) && final.Identity == selectedIdentity && current() ? result : null;
        }

        internal static void Act(Func<bool> current, Func<bool> valid, Action invoke)
        {
            Require(current() && valid() && current(), "Scratch action changed before invocation");
            invoke();
            Require(current(), "Scratch focus changed during action invocation");
        }

        internal static void Dispatch(string scope, Func<bool> current, Action prepare, Action post)
        {
            Require(Converts(scope), "Setup-only scopes cannot dispatch resident conversions");
            Require(current(), "Scratch comment lost focus before conversion preparation");
            prepare();
            Require(current(), "Scratch comment changed before conversion dispatch");
            post();
            Require(current(), "Scratch comment changed during conversion dispatch");
        }

        internal static void OpenView(Func<bool> current, Func<bool> visible, Action open, Action pause)
        {
            Require(current(), "Scratch focus changed before view discovery");
            bool alreadyVisible = visible();
            Require(current(), "Scratch focus changed during view discovery");
            if (alreadyVisible)
                return;
            Act(current, () => true, open);
            Require(Wait(current, visible, pause, 60, 2), "Comments panel did not become verifiably available");
        }

        internal static string CompleteLog(string log)
        {
            if (log == null)
                return "";
            int end = log.LastIndexOf('\n');
            return end < 0 ? "" : log.Substring(0, end + 1);
        }

        internal static string ReadLog(string path)
        {
            // The resident may append while the fixture reads. File.ReadAllText
            // denies the already-open writer; sharing must work in both directions.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                var text = new StringBuilder();
                var buffer = new char[4096];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Require(text.Length + count <= 1024 * 1024, "Recovery fixture log exceeds its bounded snapshot");
                    text.Append(buffer, 0, count);
                }

                return CompleteLog(text.ToString());
            }
        }

        internal static int LoggedHelper(string log)
        {
            var matches = Regex.Matches(CompleteLog(log), @"Word focus helper ready process=([0-9]+)\r?$", RegexOptions.Multiline);
            int identity;
            return matches.Count > 0 && int.TryParse(matches[matches.Count - 1].Groups[1].Value, out identity) && identity > 0 ? identity : 0;
        }

        internal static bool RecoveredLocator(string log, int helper)
        {
            if (helper <= 0 || log == null)
                return false;
            bool active = false, ready = false;
            foreach (string line in log.Split('\n'))
            {
                var match = Regex.Match(line, @"Word focus helper ready process=([0-9]+)\s*$");
                if (match.Success)
                {
                    active = match.Groups[1].Value == helper.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    ready = false;
                }
                else if (line.IndexOf("refreshing only the focus helper", StringComparison.Ordinal) >= 0 || line.IndexOf("Word focus helper refresh;", StringComparison.Ordinal) >= 0 || line.IndexOf("Word focus helper exited;", StringComparison.Ordinal) >= 0 || line.IndexOf("Word focus helper not ready;", StringComparison.Ordinal) >= 0 || line.IndexOf("Word focus helper unavailable:", StringComparison.Ordinal) >= 0)
                {
                    active = ready = false;
                }
                else if (active && line.IndexOf("Word focus locator resolved=", StringComparison.Ordinal) >= 0)
                    ready = line.TrimEnd().EndsWith("resolved=True", StringComparison.Ordinal);
            }

            return active && ready;
        }

        internal static string PostedCard(IEnumerable<string> ancestors)
        {
            string posted = null;
            int count = 0;
            foreach (string id in ancestors)
            {
                Require(++count <= 18, "Scratch comment ancestry exceeded its bound");
                if (id == null)
                    continue;
                if (id.StartsWith("cardRoot_", StringComparison.Ordinal) && id.Length > "cardRoot_".Length)
                    posted = id;
            }

            return posted;
        }

        internal static bool PostedMatch(string id, string expected, ISet<string> before)
        {
            if (id == null || !id.StartsWith("cardRoot_", StringComparison.Ordinal) || id.Length == "cardRoot_".Length)
                return false;
            return expected != null ? id == expected : !before.Contains(id);
        }

        internal static bool SamePostedThread(string candidate, string known)
        {
            // Native evidence: contextual cardRoot_4_947445465 becomes
            // cardRoot_1_947445465 in the panel. Only the view component changes.
            // This is used inside one verified scratch frame, never across files
            // and never to infer a posted server ID from an unposted draft.
            if (candidate == null || known == null || candidate.Length > 128 || known.Length > 128)
                return false;
            var left = Regex.Match(candidate, @"\AcardRoot_[0-9]+_([0-9]+)\z");
            var right = Regex.Match(known, @"\AcardRoot_[0-9]+_([0-9]+)\z");
            return left.Success && right.Success && left.Groups[1].Value == right.Groups[1].Value;
        }

        internal static T FindFocused<T>(IEnumerable<T> candidates, Func<T, bool> focused, Func<bool> current)
            where T : class
        {
            Require(current(), "Scratch focus changed before editor discovery");
            T result = null;
            foreach (T candidate in candidates)
            {
                Require(current(), "Scratch focus changed during editor discovery");
                if (!focused(candidate))
                    continue;
                Require(result == null, "Focused scratch comment editor is ambiguous");
                result = candidate;
            }

            Require(current(), "Scratch focus changed after editor discovery");
            return result != null && focused(result) && current() ? result : null;
        }

        internal static T Open<T>(Func<bool> current, Action activateBody, Func<bool> bodyFocused, Func<bool> commandEnabled, Action createComment, Func<T> findEditor, Func<T, bool> writable, Func<bool> bodyUnchanged, Action<string> stage)
            where T : class
        {
            Require(current(), "Scratch Word no longer owns foreground/setup");
            stage("activate-body");
            activateBody();
            Require(current() && bodyFocused(), "Scratch body did not acquire typing focus");
            stage("new-comment-command");
            Require(current() && commandEnabled() && current(), "Owned New Comment command is unavailable");
            createComment();
            Require(current(), "Scratch Word lost focus while opening comment");
            stage("find-comment-editor");
            T editor = findEditor();
            Require(editor != null && current() && writable(editor), "Owned comment editor is not writable/focused");
            Require(bodyUnchanged() && current(), "Comment setup changed its body or focus");
            stage("comment-ready");
            return editor;
        }

        static void Require(bool valid, string message)
        {
            if (!valid)
                throw new InvalidOperationException(message);
        }
    }
}
