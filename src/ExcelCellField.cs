// KeyJaya: Bounded editing adapter for an Excel cell with an active text caret.
// The owned Formula Bar mirrors the edit buffer; formulas and worksheet-grid enumeration are excluded.
using System;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace KeyJaya
{
    using System.Threading.Tasks;

    // Only one read-only provider request may remain outstanding. A late result
    // cannot authorize a subsequent edit or keep the resident UI thread blocked.
    public sealed class BoundedCellRead<T>
        where T : class
    {
        readonly object gate = new object ();
        Task<T> pending;
        public bool TryRead(Func<T> read, int milliseconds, out T result)
        {
            result = null;
            if (read == null || milliseconds < 1 || milliseconds > 1000)
                return false;
            Task<T> request;
            lock (gate)
            {
                if (pending != null && !pending.IsCompleted)
                    return false;
                request = pending = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        return read();
                    }
                    catch
                    {
                        return null;
                    }
                }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }

            try
            {
                if (!request.Wait(milliseconds))
                    return false;
                result = request.Result;
                return result != null;
            }
            finally
            {
                lock (gate)
                    if (pending == request && request.IsCompleted)
                        pending = null;
            }
        }
    }

    // Excel's EXCEL6 editor exposes no TextPattern. Its EXCEL< Formula Bar mirrors
    // the same active edit buffer and selection. Both use the same formula guard.
    // Read that owned mirror; use native input only after proving the exact range.
    public static class ExcelCellField
    {
        static int epoch;
        static readonly object gate = new object ();
        static readonly BoundedCellRead<CellState> reads = new BoundedCellRead<CellState>();
        static IntPtr mirrorOwner, modeOwner;
        static AutomationElement mirrorElement, modeElement;
        static TextPattern mirror;
        internal sealed class CellState
        {
            public string Text;
            public int Start, End;
            public string Mode;
        }

        // Native capture uses the default backend. Tests can exercise the same
        // transaction against a field model without sending desktop input.
        internal interface ICellBackend
        {
            bool Current { get; }

            bool Read(bool includeMode, out CellState state);
            bool Send(Native.Input[] keys);
            bool EnterEdit();
        }

        public static Action<string> Trace;
        static void Note(string detail)
        {
            if (Trace != null)
                Trace("Excel " + detail);
        }

        public static void Invalidate()
        {
            Interlocked.Increment(ref epoch);
        }

        internal static bool EditorClass(string name)
        {
            return name == "EXCEL6" || name == "EXCEL<";
        }

        static IntPtr Editor(IntPtr owner)
        {
            if (owner == IntPtr.Zero || owner != Native.GetForegroundWindow())
                return IntPtr.Zero;
            var root = new StringBuilder(64);
            Native.GetClassName(owner, root, 64);
            if (root.ToString() != "XLMAIN")
                return IntPtr.Zero;
            var id = Native.FocusIdentity();
            if (id == null)
                return IntPtr.Zero;
            long hwnd = (uint)id[4] | ((long)id[5] << 32);
            var handle = new IntPtr(hwnd);
            var name = new StringBuilder(64);
            Native.GetClassName(handle, name, 64);
            return EditorClass(name.ToString()) ? handle : IntPtr.Zero;
        }

        public static bool Focused(IntPtr owner)
        {
            return Editor(owner) != IntPtr.Zero;
        }

        static CacheRequest MirrorProperties()
        {
            var request = new CacheRequest
            {
                TreeScope = TreeScope.Element
            };
            request.Add(AutomationElement.ProcessIdProperty);
            request.Add(AutomationElement.AutomationIdProperty);
            request.Add(AutomationElement.ControlTypeProperty);
            request.Add(TextPattern.Pattern);
            return request;
        }

        static TextPattern Mirror(IntPtr owner)
        {
            uint pid;
            Native.GetWindowThreadProcessId(owner, out pid);
            try
            {
                if (mirrorOwner == owner && mirrorElement != null)
                {
                    var current = mirrorElement.GetUpdatedCache(MirrorProperties());
                    var info = current.Cached;
                    object pattern;
                    if (info.ProcessId == pid && info.AutomationId == "FormulaBar" && info.ControlType == ControlType.Edit && current.TryGetCachedPattern(TextPattern.Pattern, out pattern))
                        return mirror = (TextPattern)pattern;
                }
            }
            catch
            {
            }

            mirror = null;
            mirrorElement = null;
            mirrorOwner = IntPtr.Zero;
            // The Formula Bar is an immediate XLMAIN child. Descendants can enumerate
            // the worksheet's enormous virtual cell tree when the bar is hidden.
            Note("mirror discovery started");
            var root = AutomationElement.FromHandle(owner);
            var field = root.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.AutomationIdProperty, "FormulaBar"));
            object provider;
            if (field == null)
            {
                Note("Formula Bar missing");
                return null;
            }

            var cached = field.GetUpdatedCache(MirrorProperties());
            var properties = cached.Cached;
            if (properties.ProcessId != pid || properties.AutomationId != "FormulaBar" || properties.ControlType != ControlType.Edit)
            {
                Note("Formula Bar identity rejected");
                return null;
            }

            if (!cached.TryGetCachedPattern(TextPattern.Pattern, out provider))
            {
                Note("Formula Bar has no TextPattern");
                return null;
            }

            mirrorOwner = owner;
            mirrorElement = field;
            Note("mirror discovered");
            return mirror = (TextPattern)provider;
        }

        internal static bool TextBuffer(string value)
        {
            if (value == null || value.Length > 4096 || value.StartsWith("=", StringComparison.Ordinal) || value.StartsWith("+", StringComparison.Ordinal) || value.StartsWith("-", StringComparison.Ordinal) || value.StartsWith("@", StringComparison.Ordinal))
                return false;
            for (int n = 0; n < value.Length; n++)
                if (value[n] == '\r')
                {
                    if (n + 1 >= value.Length || value[n + 1] != '\n')
                        return false;
                    n++;
                }
                else if (value[n] != '\n' && char.IsControl(value[n]))
                    return false;
            return true;
        }

        internal static Native.Input[] SelectionKeys(string text, int fromStart, int fromEnd, int start, int end)
        {
            if (!TextBuffer(text) || fromStart < 0 || fromEnd < fromStart || fromEnd > text.Length)
                return null;
            var keys = Native.RelativeFieldRangeKeys(text, fromStart, fromEnd, start, end);
            // Keep the established single-line fallback for script/cluster
            // ranges. Whole-buffer Home/Shift+End needs no guessed glyph count.
            if (keys == null && text.IndexOfAny(new[] { '\r', '\n' }) < 0)
                keys = Native.FieldRangeKeys(start, end, text.Length, false);
            if (keys != null)
                for (int n = 0; n < keys.Length; n++)
                    keys[n].union.keyboard.extra = new UIntPtr(Native.OwnCorrectionInput);
            return keys;
        }

        internal static int CaretPosition(string full, int start, int end, string replacement, string tail, int offset)
        {
            if (full == null || replacement == null || tail == null || start < 0 || end < start || end > full.Length || end - start != replacement.Length || offset < 0 || offset > replacement.Length + tail.Length || tail.Length > full.Length - end || System.Linq.Enumerable.Any(tail, c => !char.IsPunctuation(c) || c == '_' || c == '/' || c == '\\' || c == '@' || c == '#') || string.CompareOrdinal(full, start, replacement, 0, replacement.Length) != 0 || string.CompareOrdinal(full, end, tail, 0, tail.Length) != 0)
                return -1;
            return start + offset;
        }

        internal static string ModeLabel(string name, string value)
        {
            if (name == "Cell Mode" && (value == "Enter" || value == "Edit" || value == "Ready"))
                return value;
            foreach (string mode in new[]
            {
                "Enter",
                "Edit",
                "Ready"
            }

            )
                if (name == "Cell Mode " + mode)
                    return mode;
            return null;
        }

        static CacheRequest ModeProperties()
        {
            var request = new CacheRequest
            {
                TreeScope = TreeScope.Element
            };
            request.Add(AutomationElement.ProcessIdProperty);
            request.Add(AutomationElement.NameProperty);
            request.Add(AutomationElement.ControlTypeProperty);
            request.Add(ValuePattern.Pattern);
            request.Add(ValuePattern.ValueProperty);
            request.Add(TextPattern.Pattern);
            return request;
        }

        static string ModeValue(AutomationElement element, uint process)
        {
            var current = element.GetUpdatedCache(ModeProperties());
            var info = current.Cached;
            if (info.ProcessId != process || info.ControlType != ControlType.Text)
                return null;
            string name = info.Name;
            if (name == null || !name.StartsWith("Cell Mode", StringComparison.Ordinal))
                return null;
            object provider;
            string value = null;
            if (current.TryGetCachedPattern(ValuePattern.Pattern, out provider))
                value = ((ValuePattern)provider).Cached.Value;
            else if (current.TryGetCachedPattern(TextPattern.Pattern, out provider))
                value = ((TextPattern)provider).DocumentRange.GetText(32);
            string mode = ModeLabel(name, value);
            if (mode == null)
                Note("cell-mode label found without a recognized value");
            return mode;
        }

        static string ReadMode(IntPtr owner)
        {
            uint pid;
            Native.GetWindowThreadProcessId(owner, out pid);
            try
            {
                if (modeOwner == owner && modeElement != null)
                    return ModeValue(modeElement, pid);
            }
            catch
            {
            }

            modeElement = null;
            modeOwner = IntPtr.Zero;
            // Search only shallow toolbar/status containers, never the virtual cell grid.
            var nodes = new System.Collections.Generic.Stack<Tuple<AutomationElement, int>>();
            nodes.Push(Tuple.Create(AutomationElement.FromHandle(owner), 0));
            int remaining = 192;
            while (nodes.Count > 0 && remaining-- > 0)
            {
                var item = nodes.Pop();
                var node = item.Item1;
                var type = node.Current.ControlType;
                if (type == ControlType.Text)
                {
                    string mode = ModeValue(node, pid);
                    if (mode != null)
                    {
                        modeOwner = owner;
                        modeElement = node;
                        Note("cell mode discovered " + mode);
                        return mode;
                    }

                    continue;
                }

                if (item.Item2 >= 8 || type != ControlType.Window && type != ControlType.Pane && type != ControlType.ToolBar && type != ControlType.StatusBar)
                    continue;
                var children = node.FindAll(TreeScope.Children, Condition.TrueCondition);
                for (int n = Math.Min(children.Count, 64) - 1; n >= 0; n--)
                    nodes.Push(Tuple.Create(children[n], item.Item2 + 1));
            }

            Note("cell mode unavailable");
            return null;
        }

        // A collapsed caret has one endpoint and no selected text. Do not ask
        // Excel for the same prefix twice; ranges still require both endpoints
        // and exact selected text. Callbacks let tests count real read decisions.
        internal static bool ReadOffsets(string value, bool collapsed, Func<int> readStart, Func<int> readEnd, Func<string> readSelected, out int start, out int end)
        {
            start = end = -1;
            if (!TextBuffer(value))
                return false;
            int begin = readStart(), stop = collapsed ? begin : readEnd();
            if (begin < 0 || stop < begin || stop > value.Length || !collapsed && readSelected() != value.Substring(begin, stop - begin))
                return false;
            start = begin;
            end = stop;
            return true;
        }

        static bool Read(TextPattern pattern, out string text, out int start, out int end)
        {
            text = null;
            start = end = -1;
            var document = pattern.DocumentRange;
            string value = document.GetText(4097);
            if (!TextBuffer(value))
            {
                Note("buffer rejected");
                return false;
            }

            var selected = pattern.GetSelection();
            if (selected.Length != 1 || object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), true))
            {
                Note("selection unavailable or read-only count=" + selected.Length);
                return false;
            }

            var prefix = document.Clone();
            bool collapsed = selected[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End) == 0;
            if (!ReadOffsets(value, collapsed, () =>
            {
                prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End, selected[0], TextPatternRangeEndpoint.Start);
                return prefix.GetText(4097).Length;
            }, () =>
            {
                prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End, selected[0], TextPatternRangeEndpoint.End);
                return prefix.GetText(4097).Length;
            }, () => selected[0].GetText(4097), out start, out end))
            {
                Note("range rejected");
                return false;
            }

            text = value;
            return true;
        }

        static CellState ReadCell(IntPtr owner, IntPtr editor, int stamp, bool includeMode)
        {
            try
            {
                if (stamp != epoch || Editor(owner) != editor)
                {
                    Note("read invalidated before discovery");
                    return null;
                }

                lock (gate)
                {
                    if (stamp != epoch || Editor(owner) != editor)
                        return null;
                    var pattern = Mirror(owner);
                    string text;
                    int start, end;
                    if (pattern == null || !Read(pattern, out text, out start, out end))
                        return null;
                    string mode = includeMode ? ReadMode(owner) : null;
                    if (stamp != epoch || Editor(owner) != editor)
                    {
                        Note("read invalidated after discovery");
                        return null;
                    }

                    return new CellState
                    {
                        Text = text,
                        Start = start,
                        End = end,
                        Mode = mode
                    };
                }
            }
            catch (Exception ex)
            {
                Note("provider read failed " + ex.GetType().Name);
                return null;
            }
        }

        static bool TryReadCell(IntPtr owner, IntPtr editor, int stamp, out CellState state, bool includeMode = false)
        {
            state = null;
            if (stamp != epoch || Editor(owner) != editor)
            {
                Note("read invalidated before scheduling");
                return false;
            }

            CellState value;
            bool read = reads.TryRead(() => ReadCell(owner, editor, stamp, includeMode), mirror == null || includeMode && modeElement == null ? 1000 : 250, out value);
            if (!read || stamp != epoch || Editor(owner) != editor)
            {
                if (!read && Trace != null)
                    Trace("Excel cell read unavailable or exceeded time limit");
                return false;
            }

            state = value;
            return true;
        }

        public static Snapshot Capture(IntPtr owner)
        {
            IntPtr editor = Editor(owner);
            if (editor == IntPtr.Zero)
                return null;
            try
            {
                int stamp = epoch;
                CellState state;
                if (!TryReadCell(owner, editor, stamp, out state))
                    return null;
                return new Snapshot
                {
                    Owner = owner,
                    EditorHandle = editor,
                    Stamp = stamp,
                    Full = state.Text,
                    Start = state.Start,
                    End = state.End,
                    SelectionStart = state.Start,
                    SelectionEnd = state.End,
                    OriginalStart = state.Start,
                    OriginalEnd = state.End
                };
            }
            catch (Exception ex)
            {
                if (Trace != null)
                    Trace("Excel mirror capture skipped: " + ex.GetType().Name);
                return null;
            }
        }

        public sealed class Snapshot
        {
            public IntPtr Owner;
            internal IntPtr EditorHandle;
            internal int Stamp;
            public string Full;
            public int Start, End, SelectionStart, SelectionEnd;
            internal int OriginalStart, OriginalEnd;
            internal string Tail = "";
            internal ICellBackend Backend { get; set; }

            public string Text
            {
                get
                {
                    return Full.Substring(Start, End - Start);
                }
            }

            public bool Current
            {
                get
                {
                    return Backend != null ? Backend.Current : Stamp == epoch && Editor(Owner) == EditorHandle;
                }
            }

            public Snapshot Clone()
            {
                return (Snapshot)MemberwiseClone();
            }

            bool ReadState(out CellState state, bool mode = false)
            {
                state = null;
                if (!Current)
                    return false;
                CellState value;
                bool read = Backend != null ? Backend.Read(mode, out value) : TryReadCell(Owner, EditorHandle, Stamp, out value, mode);
                if (!read || value == null || !Current)
                    return false;
                state = value;
                return true;
            }

            bool Send(Native.Input[] keys)
            {
                if (keys == null || !Current)
                    return false;
                for (int n = 0; n < keys.Length; n++)
                    keys[n].union.keyboard.extra = new UIntPtr(Native.OwnCorrectionInput);
                return Backend != null ? Backend.Send(keys) : Native.ReplayCorrectionInput(keys);
            }

            bool At(string text, int start, int end)
            {
                try
                {
                    if (!Current)
                        return false;
                    CellState actual;
                    return ReadState(out actual) && actual.Text == text && actual.Start == start && actual.End == end && Current;
                }
                catch
                {
                    return false;
                }
            }

            bool WaitAt(string text, int start, int end, Func<bool> context = null)
            {
                for (int n = 0; n < 25; n++)
                {
                    if (!Current || context != null && !context())
                        return false;
                    if (At(text, start, end))
                        return true;
                    Thread.Sleep(5);
                }

                return false;
            }

            public bool Matches(bool caret)
            {
                return caret ? SelectionStart == SelectionEnd && SelectionStart >= Start && SelectionEnd <= End + Tail.Length && At(Full, SelectionStart, SelectionEnd) : SelectionStart == Start && SelectionEnd == End && At(Full, Start, End);
            }

            public bool SameStart(Snapshot other)
            {
                return other != null && Owner == other.Owner && EditorHandle == other.EditorHandle && Start == other.Start && Full.Substring(0, Start) == other.Full.Substring(0, other.Start) && Full.Substring(End) == other.Full.Substring(other.End) && Current;
            }

            public string Key
            {
                get
                {
                    return Owner + ":excel:" + EditorHandle + ":" + Stamp + ":" + Start;
                }
            }

            // F2 toggles Enter/Edit. Use it only when the owned status bar proves Enter;
            // otherwise Home/End can commit a cell instead of navigating its text.
            bool EnsureEdit(int begin, int end)
            {
                CellState actual;
                if (!Current || !ReadState(out actual, true) || actual.Text != Full || actual.Start != begin || actual.End != end)
                    return false;
                if (actual.Mode == "Edit")
                    return true;
                if (actual.Mode != "Enter" || !Current || !(Backend != null ? Backend.EnterEdit() : Native.Chord(0x71)))
                    return false;
                for (int n = 0; n < 25 && Current; n++)
                {
                    if (ReadState(out actual, true) && actual.Text == Full && actual.Start == begin && actual.End == end && actual.Mode == "Edit")
                        return true;
                    Thread.Sleep(5);
                }

                return false;
            }

            public bool Select()
            {
                var keys = SelectionKeys(Full, SelectionStart, SelectionEnd, Start, End);
                if (keys == null || !EnsureEdit(SelectionStart, SelectionEnd) || !Send(keys) || !WaitAt(Full, Start, End))
                    return false;
                SelectionStart = Start;
                SelectionEnd = End;
                return true;
            }

            public bool Restore()
            {
                if (!Current)
                    return false;
                if (At(Full, OriginalStart, OriginalEnd))
                {
                    SelectionStart = OriginalStart;
                    SelectionEnd = OriginalEnd;
                    return true;
                }

                CellState actual;
                if (!ReadState(out actual) || actual.Text != Full)
                    return false;
                var keys = SelectionKeys(Full, actual.Start, actual.End, OriginalStart, OriginalEnd);
                if (keys == null || !EnsureEdit(actual.Start, actual.End) || !Send(keys) || !WaitAt(Full, OriginalStart, OriginalEnd))
                    return false;
                SelectionStart = OriginalStart;
                SelectionEnd = OriginalEnd;
                return true;
            }

            public bool Position(string replacement, int offset)
            {
                int at = CaretPosition(Full, Start, End, replacement, Tail, offset);
                var keys = at < 0 ? null : SelectionKeys(Full, SelectionStart, SelectionEnd, at, at);
                if (!Current || keys == null || !EnsureEdit(SelectionStart, SelectionEnd))
                    return false;
                if (!Send(keys) || !WaitAt(Full, at, at))
                    return false;
                SelectionStart = SelectionEnd = at;
                return true;
            }

            public bool Replace(string output, bool retain, Func<bool> context)
            {
                var keys = Native.ExcelTextKeys(output);
                if (keys == null)
                    return false;
                if (!context())
                    return false;
                string wanted = Full.Substring(0, Start) + output + Full.Substring(End);
                var retained = SelectionKeys(wanted, Start + output.Length, Start + output.Length, Start, Start + output.Length);
                // EnsureEdit proves the same full buffer and selected range;
                // a separate preceding Matches read would duplicate that proof.
                if (!TextBuffer(wanted) || retained == null || !EnsureEdit(Start, End))
                    return false;
                if (!context() || !Matches(false) || !Send(keys) || !WaitAt(wanted, Start + output.Length, Start + output.Length, context))
                    return false;
                Full = wanted;
                End = Start + output.Length;
                SelectionStart = SelectionEnd = End;
                if (retain)
                {
                    if (!context() || !Current || !Send(retained) || !WaitAt(Full, Start, End, context))
                        return false;
                    SelectionStart = Start;
                }

                return true;
            }

            public bool Resume(string output, bool selected, bool typing)
            {
                if (!Current)
                    return false;
                if (output == null)
                    return selected && typing ? Position(Text, Text.Length) : Restore();
                if (Text != output)
                    return false;
                if (selected && typing)
                    return Position(output, output.Length);
                return At(Full, SelectionStart, SelectionEnd);
            }
        }
    }
}
