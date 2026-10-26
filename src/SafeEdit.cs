// KeyJaya: Verified editor targets, caret/range discovery, and Word-owned paste-popup focus handling.
// A captured range is evidence, not permission: recheck ownership and content before committing an edit.
using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Globalization;

namespace KeyJaya
{
    /// <summary>
    /// Captures one edit target and the evidence needed to select, replace, and
    /// recover it. Window/focus identity, expected text, and caret proofs expire
    /// independently; a stale provider object alone must never authorize a write.
    /// </summary>
    public sealed class SafeEdit : ISelectionSurface
    {
        public static Action<string> Trace;
        static void Detail(string message)
        {
            if (Trace != null)
                Trace(message);
        }

        public sealed class CaretTarget
        {
            public readonly string Text, Font;
            public readonly bool Mixed;
            public readonly SafeEdit Edit;
            public CaretTarget(string text, string font, bool mixed = false, SafeEdit edit = null)
            {
                Text = text;
                Font = font;
                Mixed = mixed;
                Edit = edit;
            }
        }

        static SafeEdit InDesignEdit(InDesignSelection.Snapshot snapshot, bool letter = false, bool selected = false)
        {
            return snapshot == null ? null : new SafeEdit(snapshot.Owner, null, snapshot.Text, null, letter)
            {
                inDesign = snapshot,
                capturedCaret = !selected,
                userSelection = selected,
                CaretOffset = snapshot.Offset,
                caretTail = snapshot.Tail
            };
        }

        public static SafeEdit InDesignInsertion(IntPtr window)
        {
            var snap = InDesignSelection.Read(window);
            return snap != null && snap.SelectionStart == snap.SelectionEnd ? InDesignEdit(snap, false, true) : null;
        }

        bool? markdownSource;
        public bool IsMarkdownSource
        {
            get
            {
                if (markdownSource.HasValue)
                    return markdownSource.Value;
                markdownSource = DetectMarkdownSource();
                return markdownSource.Value;
            }
        }

        bool DetectMarkdownSource()
        {
            try
            {
                uint pid;
                Native.GetWindowThreadProcessId(Window, out pid);
                if (!Native.ProcessImageIs(pid, "Obsidian.exe") || range == null || object.Equals(range.GetAttributeValue(TextPattern.IsReadOnlyAttribute), true))
                    return false;
                var node = focused;
                for (int n = 0; n < 12 && node != null; n++)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(node.Current.ClassName ?? "", @"(?:cm-content|cm-editor|CodeMirror)"))
                        return true;
                    node = TreeWalker.ControlViewWalker.GetParent(node);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public string MarkdownPrefix
        {
            get
            {
                return IsMarkdownSource ? StartPrefix() : null;
            }
        }

        public string MarkdownSuffix
        {
            get
            {
                try
                {
                    if (!IsMarkdownSource)
                        return "";
                    var after = range.Clone();
                    after.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 321);
                    string text = Lines(after.GetText(-1));
                    return text.StartsWith(expected, StringComparison.Ordinal) ? text.Substring(expected.Length) : "";
                }
                catch
                {
                    return "";
                }
            }
        }

        public bool MarkdownAllowed
        {
            get
            {
                if (!IsMarkdownSource)
                    return true;
                string prefix = MarkdownPrefix;
                if (prefix == null || MarkdownFormatting.InFence(prefix))
                    return false;
                string line = prefix.Substring(prefix.LastIndexOf('\n') + 1);
                return line.LastIndexOf('[') <= line.LastIndexOf(']') && System.Text.RegularExpressions.Regex.Matches(line, @"`+").Count % 2 == 0;
            }
        }

        public string MarkdownConvert(string input, Func<string, string> convert)
        {
            if (findField != null && findField.Text == input)
                return InDesignFindSyntax.ConvertRange(findField.Full, findField.Start, findField.End, convert);
            if (inDesignField)
                return InDesignFindSyntax.ConvertLiterals(input, convert);
            return !IsMarkdownSource ? convert(input) : !MarkdownAllowed ? input : MarkdownFormatting.ConvertProtected(input, convert);
        }

        public string MarkdownOutput(string output, Core core, string mode, string trigger = null, bool individual = false)
        {
            if (!IsMarkdownSource || !MarkdownAllowed)
                return output;
            if (output.Length == 0)
                return output;
            bool wrapped = expected.Length > 2 && expected[0] == '*' && expected[expected.Length - 1] == '*';
            if (wrapped && output[0] != '*')
                output = "*" + output + "*";
            try
            {
                if (individual && range != null && object.Equals(range.GetAttributeValue(TextPattern.IsItalicAttribute), true) && !core.ControlItalics(MarkdownWord(expected), output, mode, true).Any(x => !x.Italic))
                    return output;
            }
            catch
            {
            }

            return MarkdownFormatting.Apply(output, value => trigger == null ? core.ControlItalics(MarkdownWord(expected), value, mode, individual) : core.TriggerItalics(trigger, value, mode), MarkdownPrefix ?? "", MarkdownSuffix);
        }

        public string DisplayedOutput(string output)
        {
            return editHandle != IntPtr.Zero ? Native.EditTextCoordinates(editHandle, output) : !IsMarkdownSource ? output : MarkdownFormatting.DisplayText(output);
        }

        public bool RepresentsOutput(SafeEdit selected, string output)
        {
            return selected != null && (selected.Expected == output || selected.Expected == DisplayedOutput(output)) && SameStart(selected);
        }

        public static string MarkdownWord(string word)
        {
            if (word == null || word.Length < 3 || word[0] != '*' || word[word.Length - 1] != '*' || word.StartsWith("**", StringComparison.Ordinal))
                return word;
            string inner = word.Substring(1, word.Length - 2);
            return CaretWords.AtEnd(inner) == inner ? inner : word;
        }

        public static string CycleMarkdownWord(Core core, string input, string mode)
        {
            string word = MarkdownWord(input);
            var choices = core.CaretChoices(EncodingMap.Ascii(word), mode);
            if (choices.Length == 0)
                choices = core.CaretChoices(word, mode);
            if (choices.Length == 0)
                return core.ConvertHighlighted(word, mode);
            int index = Array.IndexOf(choices, word);
            return choices[index < 0 ? 0 : (index + 1) % choices.Length];
        }

        void ExpandMarkdownCaret()
        {
            if (!IsMarkdownSource || !MarkdownAllowed)
                return;
            string prefix = MarkdownPrefix, suffix = MarkdownSuffix;
            if (prefix == null || !prefix.EndsWith("*", StringComparison.Ordinal) || prefix.EndsWith("**", StringComparison.Ordinal) || !suffix.StartsWith("*", StringComparison.Ordinal) || suffix.StartsWith("**", StringComparison.Ordinal))
                return;
            var expanded = range.Clone();
            expanded.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
            expanded.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 1);
            string wrapped = "*" + expected + "*";
            if (Lines(expanded.GetText(-1)) != wrapped)
            {
                Detail("markdown wrapper expansion rejected: provider text differs");
                return;
            }

            range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, expanded, TextPatternRangeEndpoint.Start);
            range.MoveEndpointByRange(TextPatternRangeEndpoint.End, expanded, TextPatternRangeEndpoint.End);
            expected = wrapped;
            CaretOffset++;
            if (caretTail.StartsWith("*", StringComparison.Ordinal))
                caretTail = caretTail.Substring(1);
        }

        public bool ExpandedMarkdownSelection { get; private set; }

        public void ExpandSelectedMarkdown()
        {
            if (!userSelection || ExpandedMarkdownSelection || !IsMarkdownSource)
                return;
            string before = expected;
            ExpandMarkdownCaret();
            ExpandedMarkdownSelection = before != expected;
        }

        public bool IsInDesign
        {
            get
            {
                return inDesign != null || findField != null;
            }
        }

        InDesignSelection.Snapshot inDesign;
        InDesignFindField.Snapshot findField;
        ExcelCellField.Snapshot excelCell;
        internal bool IsExcelCell
        {
            get
            {
                return excelCell != null;
            }
        }

        public bool UsesNativeReplacement
        {
            get
            {
                return IsInDesign || excelCell != null;
            }
        }

        public bool SupportsNativeItalics
        {
            get
            {
                return inDesign != null;
            }
        }

        bool readInitialItalic;
        bool? initialItalic;
        public bool? InitialItalic
        {
            get
            {
                if (!readInitialItalic)
                {
                    readInitialItalic = true;
                    try
                    {
                        object value = range == null ? null : range.GetAttributeValue(TextPattern.IsItalicAttribute);
                        if (value is bool)
                            initialItalic = (bool)value;
                    }
                    catch (Exception)
                    {
                    }
                }

                return initialItalic;
            }
        }

        public bool ReplaceInDesign(string output, bool retain, ItalicSpan[] italics = null, SafeEdit restore = null)
        {
            if (excelCell != null)
                return editContext != null && excelCell.Replace(output, retain, editContext);
            if (findField != null)
                return editContext != null && findField.Replace(output, retain, editContext);
            return inDesign != null && editContext != null && editContext() && inDesign.Replace(output, retain, italics, restore == null ? null : restore.inDesign);
        }

        static CaretTarget InDesignCaret(IntPtr window, bool letter, bool componentOnly)
        {
            var snap = InDesignSelection.Read(window);
            if (snap == null || snap.SelectionStart != snap.SelectionEnd)
                return null;
            string text = snap.Left + snap.Right;
            int caret = snap.Left.Length;
            if (letter)
            {
                if (caret == 0)
                    return null;
                snap.Start = snap.SelectionStart - 1;
                snap.End = snap.SelectionEnd;
                snap.Text = text.Substring(caret - 1, 1);
                snap.Offset = 1;
            }
            else
            {
                var span = CaretWords.Locate(text, caret, caret, componentOnly);
                if (span == null)
                    return null;
                snap.Start = snap.SelectionStart + span.Start - caret;
                snap.End = snap.Start + span.Word.Length;
                snap.Text = span.Word;
                snap.Offset = span.Offset;
                snap.Tail = span.Tail;
            }

            if (!InDesignSelection.Describe(snap))
                return null;
            return new CaretTarget(snap.Text, snap.Font.Split('\t')[0], snap.Fonts.Select(f => f.Split('\t')[0]).Distinct().Count() > 1, InDesignEdit(snap, letter && SingleLetters.Supported(snap.Text)));
        }

        static SafeEdit FindEdit(InDesignFindField.Snapshot snapshot, bool letter = false, bool selected = false)
        {
            return snapshot == null ? null : new SafeEdit(snapshot.Owner, null, snapshot.Text, null, letter)
            {
                findField = snapshot,
                capturedCaret = !selected,
                userSelection = selected,
                CaretOffset = snapshot.SelectionStart - snapshot.Start,
                caretTail = ""
            };
        }

        static CaretTarget FindCaret(IntPtr window, bool letter, bool componentOnly)
        {
            var snap = InDesignFindField.Capture(window);
            if (snap == null || snap.SelectionStart != snap.SelectionEnd)
                return null;
            int caret = snap.SelectionStart;
            if (letter)
            {
                if (caret == 0)
                    return null;
                snap.Start = caret - 1;
                snap.End = caret;
            }
            else
            {
                var span = CaretWords.Locate(snap.Full, caret, caret, componentOnly);
                if (span == null)
                    return null;
                snap.Start = span.Start;
                snap.End = span.Start + span.Word.Length;
            }

            if (!InDesignFindSyntax.IsLiteralRange(snap.Full, snap.Start, snap.End))
                return null;
            return new CaretTarget(snap.Text, null, false, FindEdit(snap, letter && SingleLetters.Supported(snap.Text)));
        }

        static SafeEdit ExcelEdit(ExcelCellField.Snapshot snap, bool letter = false, bool selected = false)
        {
            // A selected passage is accepted only if both its insertion and
            // later retained selection have a verified keyboard plan.
            if (snap != null && selected && (Native.ExcelTextKeys(snap.Text) == null || ExcelCellField.SelectionKeys(snap.Full, snap.End, snap.End, snap.Start, snap.End) == null))
                return null;
            return snap == null ? null : new SafeEdit(snap.Owner, null, snap.Text, null, letter)
            {
                excelCell = snap,
                capturedCaret = !selected,
                userSelection = selected,
                CaretOffset = snap.SelectionEnd - snap.Start,
                caretTail = snap.Tail
            };
        }

        static CaretTarget ExcelCaret(IntPtr window, bool letter, bool componentOnly)
        {
            return ExcelCaret(ExcelCellField.Capture(window), letter, componentOnly);
        }

        static CaretTarget ExcelCaret(ExcelCellField.Snapshot snap, bool letter, bool componentOnly)
        {
            if (snap == null || snap.SelectionStart != snap.SelectionEnd)
                return null;
            int caret = snap.SelectionStart;
            if (letter)
            {
                if (caret == 0)
                    return null;
                snap.Start = caret - 1;
                snap.End = caret;
            }
            else
            {
                var span = CaretWords.Locate(snap.Full, caret, caret, componentOnly);
                if (span == null)
                    return null;
                snap.Start = span.Start;
                snap.End = span.Start + span.Word.Length;
                snap.Tail = span.Tail;
            }

            return new CaretTarget(snap.Text, null, false, ExcelEdit(snap, letter && SingleLetters.Supported(snap.Text)));
        }

        // One discovery read can describe either selection or caret. The edit
        // adapter still rechecks live text, range, mode and owner before input.
        internal static SafeEdit ReadExcelDiscovery(Func<ExcelCellField.Snapshot> capture, out bool selectionPresent, out CaretTarget caret)
        {
            var snap = capture();
            caret = null;
            selectionPresent = snap == null || snap.SelectionStart != snap.SelectionEnd;
            if (snap == null)
                return null;
            if (selectionPresent)
                return ExcelEdit(snap, false, true);
            caret = ExcelCaret(snap, false, false);
            return null;
        }

        internal static SafeEdit ReadExcelInsertionOrSelection(Func<ExcelCellField.Snapshot> capture)
        {
            var snap = capture();
            return snap == null ? null : ExcelEdit(snap, false, snap.SelectionStart != snap.SelectionEnd);
        }

        static bool FindCaretWordRange(TextPatternRange left, TextPatternRange right, TextPatternRange caret, bool componentOnly, out TextPatternRange result, out CaretWordSpan span)
        {
            result = null;
            span = null;
            var context = left.Clone();
            context.MoveEndpointByRange(TextPatternRangeEndpoint.End, right, TextPatternRangeEndpoint.End);
            string text = context.GetText(4097), prefix = left.GetText(4097);
            // Read one contiguous context: joining two GetText results can omit a
            // browser's implicit separator at an inline/paragraph boundary.
            if (text.Length > 4096 || prefix.Length > 4096 || !text.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            span = CaretWords.Locate(text, prefix.Length, prefix.Length, componentOnly);
            if (span == null)
                return false;
            var search = context.Clone();
            for (int n = 0; n < 512; n++)
            {
                var found = search.FindText(span.Word, false, false);
                if (found == null || found.CompareEndpoints(TextPatternRangeEndpoint.Start, caret, TextPatternRangeEndpoint.Start) > 0)
                    return false;
                if (found.GetText(161) == span.Word)
                {
                    var before = found.Clone();
                    before.MoveEndpointByRange(TextPatternRangeEndpoint.End, caret, TextPatternRangeEndpoint.Start);
                    var after = found.Clone();
                    after.MoveEndpointByRange(TextPatternRangeEndpoint.Start, caret, TextPatternRangeEndpoint.End);
                    if (CaretWords.RangeAtCaret(span.Word, span.Offset, span.Tail, before.GetText(193), after.GetText(161)))
                    {
                        result = found;
                        Detail("caret range recovered from contiguous context");
                        return true;
                    }
                }

                if (found.CompareEndpoints(TextPatternRangeEndpoint.End, search, TextPatternRangeEndpoint.Start) <= 0)
                    return false;
                search.MoveEndpointByRange(TextPatternRangeEndpoint.Start, found, TextPatternRangeEndpoint.End);
            }

            return false;
        }

        static void BoundCaretParagraph(TextPatternRange left, TextPatternRange right, TextPatternRange caret)
        {
            try
            {
                // Firefox can omit the separator between an editable field and page
                // labels in GetText. Never let that seam turn the field's first/last
                // word into part of a neighboring heading. Paragraph units retain the
                // provider's structural boundary, including in a one-word field.
                var paragraph = caret.Clone();
                paragraph.ExpandToEnclosingUnit(TextUnit.Paragraph);
                if (paragraph.CompareEndpoints(TextPatternRangeEndpoint.Start, caret, TextPatternRangeEndpoint.Start) > 0 || paragraph.CompareEndpoints(TextPatternRangeEndpoint.End, caret, TextPatternRangeEndpoint.End) < 0)
                    return;
                if (paragraph.CompareEndpoints(TextPatternRangeEndpoint.Start, left, TextPatternRangeEndpoint.Start) > 0)
                    left.MoveEndpointByRange(TextPatternRangeEndpoint.Start, paragraph, TextPatternRangeEndpoint.Start);
                if (paragraph.CompareEndpoints(TextPatternRangeEndpoint.End, right, TextPatternRangeEndpoint.End) < 0)
                    right.MoveEndpointByRange(TextPatternRangeEndpoint.End, paragraph, TextPatternRangeEndpoint.End);
            }
            catch (InvalidOperationException)
            {
            }
            catch (COMException)
            {
            }
        }

        public static CaretTarget ReadCaret(IntPtr window, bool letter, bool componentOnly = false)
        {
            if (ExcelCellField.Focused(window))
                return ExcelCaret(window, letter, componentOnly);
            if (InDesignFindField.Focused(window))
                return FindCaret(window, letter, componentOnly);
            if (InDesignSelection.UseDocumentBridge(window))
                return InDesignCaret(window, letter, componentOnly);
            try
            {
                if (window != Native.GetForegroundWindow())
                    return null;
                var focus = EmbeddedWebViewFocus.Focused(window);
                if (focus == null)
                {
                    Detail("caret focused provider unavailable");
                    return null;
                }

                object provider;
                if (focus.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                {
                    TextPattern pattern = (TextPattern)provider;
                    var selected = pattern.GetSelection();
                    if (selected.Length != 1 || selected[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End) != 0)
                    {
                        Detail("caret selection not collapsed count=" + selected.Length);
                        return null;
                    }

                    if (object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), true))
                    {
                        Detail("caret provider is read-only");
                        return null;
                    }

                    TextPatternRange caret = selected[0].Clone(), range = caret.Clone();
                    int offset;
                    string value, tail = "";
                    if (letter)
                    {
                        range.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
                        value = range.GetText(-1);
                        if (value.Length > 1)
                            return null;
                        offset = value.Length;
                    }
                    else
                    {
                        TextPatternRange left = caret.Clone(), right = caret.Clone();
                        left.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -321);
                        right.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 321);
                        BoundCaretParagraph(left, right, caret);
                        string prefix = left.GetText(-1), suffix = right.GetText(-1);
                        Detail("caret paragraph context leftCharacters=" + prefix.Length + " rightCharacters=" + suffix.Length);
                        var span = CaretWords.Locate(prefix + suffix, prefix.Length, prefix.Length, componentOnly);
                        bool exact = false;
                        if (span != null)
                        {
                            range.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, span.Start - prefix.Length);
                            range.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, span.End - prefix.Length);
                            exact = range.GetText(-1) == span.Word;
                        }

                        if (!exact && !FindCaretWordRange(left, right, caret, componentOnly, out range, out span))
                        {
                            Detail("caret range unavailable leftCharacters=" + prefix.Length + " rightCharacters=" + suffix.Length);
                            return null;
                        }

                        value = span.Word;
                        offset = span.Offset;
                        tail = span.Tail;
                    }

                    object font = range.GetAttributeValue(TextPattern.FontNameAttribute);
                    var edit = new SafeEdit(window, range, value, pattern, letter && SingleLetters.Supported(value))
                    {
                        focused = focus,
                        capturedCaret = true,
                        caretAtRead = caret,
                        CaretOffset = offset,
                        caretTail = tail
                    };
                    if (!letter)
                        edit.ExpandMarkdownCaret();
                    return new CaretTarget(edit.Expected, edit.IsInDesignField ? null : font as string, !edit.IsInDesignField && font == TextPattern.MixedAttributeValue, edit);
                }

                IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
                if (handle == IntPtr.Zero)
                    return null;
                var kind = new StringBuilder(128);
                Native.GetClassName(handle, kind, 128);
                bool simple = kind.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) || kind.ToString().StartsWith("WindowsForms10.EDIT.", StringComparison.OrdinalIgnoreCase);
                if (!simple && !kind.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
                    return null;
                int caretStart, caretEnd;
                Native.GetEditSelection(handle, out caretStart, out caretEnd);
                if (caretStart != caretEnd || caretStart < 0)
                    return null;
                string text;
                if (!Native.TryReadEditText(handle, out text) || caretStart > text.Length)
                    return null;
                string valueNative, tailNative = "";
                int begin, endNative, offsetNative;
                if (letter)
                {
                    begin = Math.Max(0, caretStart - 1);
                    endNative = caretStart;
                    valueNative = text.Substring(begin, endNative - begin);
                    offsetNative = valueNative.Length;
                }
                else
                {
                    var span = CaretWords.Locate(text, caretStart, caretEnd, componentOnly);
                    if (span == null)
                        return null;
                    begin = span.Start;
                    endNative = span.End;
                    valueNative = span.Word;
                    offsetNative = span.Offset;
                    tailNative = span.Tail;
                }

                string name = null;
                if (simple)
                {
                    IntPtr fontHandle = Native.SendMessage(handle, 0x31, IntPtr.Zero, IntPtr.Zero);
                    if (fontHandle != IntPtr.Zero)
                        using (Font font = Font.FromHfont(fontHandle))
                            name = font.Name;
                }

                var nativeEdit = new SafeEdit(window, null, valueNative, null, letter && SingleLetters.Supported(valueNative))
                {
                    focused = focus,
                    editHandle = handle,
                    editStart = begin,
                    editEnd = endNative,
                    capturedCaret = true,
                    caretAtReadNative = caretStart,
                    CaretOffset = offsetNative,
                    caretTail = tailNative
                };
                return new CaretTarget(valueNative, name, false, nativeEdit);
            }
            catch (Exception ex)
            {
                Detail("caret provider failure=" + ex.GetType().Name);
                return null;
            }
        }

        public static string LetterBeforeCaret(IntPtr window)
        {
            if (InDesignSelection.UseDocumentBridge(window))
            {
                var value = InDesignCaret(window, true, false);
                return value == null ? null : value.Text;
            }

            try
            {
                if (window != Native.GetForegroundWindow())
                    return null;
                var focus = EmbeddedWebViewFocus.Focused(window);
                if (focus == null)
                    return null;
                object obj;
                if (focus.TryGetCurrentPattern(TextPattern.Pattern, out obj))
                {
                    var selected = ((TextPattern)obj).GetSelection();
                    if (selected.Length != 1 || selected[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End) != 0)
                        return null;
                    var previous = selected[0].Clone();
                    previous.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
                    string letter = previous.GetText(2);
                    return SingleLetters.Supported(letter) ? letter : null;
                }

                IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
                if (handle == IntPtr.Zero)
                    return null;
                var kind = new StringBuilder(128);
                Native.GetClassName(handle, kind, 128);
                if (!kind.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) && !kind.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
                    return null;
                int start, end;
                Native.GetEditSelection(handle, out start, out end);
                if (start != end || start < 1)
                    return null;
                string text;
                if (!Native.TryReadEditText(handle, out text) || start > text.Length)
                    return null;
                string found = text[start - 1].ToString();
                return SingleLetters.Supported(found) ? found : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool UserSelection
        {
            get
            {
                return userSelection;
            }
        }

        bool userSelection, capturedCaret;
        TextPatternRange caretAtRead;
        int caretAtReadNative;
        string caretTail = "";
        public int CaretOffset { get; private set; }

        public string CaretTail
        {
            get
            {
                return caretTail;
            }
        }

        public readonly bool SingleLetter;
        public readonly IntPtr Window;
        readonly TextPatternRange range;
        string expected;
        public string Expected
        {
            get
            {
                return expected;
            }
        }

        readonly TextPattern pattern;
        SelectionAnchor stableStart;
        AutomationElement focused;
        IntPtr editHandle;
        int editStart, editEnd;
        int[] capturedFocus;
        int[] FocusIdentity()
        {
            return capturedFocus ?? (capturedFocus = focused == null ? null : focused.GetRuntimeId());
        }

        WordSelection.OwnedTextAnchor wordLetter;
        readonly bool inDesignField;
        public bool IsInDesignField
        {
            get
            {
                return inDesignField;
            }
        }

        SafeEdit(IntPtr window, TextPatternRange r, string text, TextPattern provider = null, bool singleLetter = false)
        {
            SingleLetter = singleLetter;
            Window = window;
            range = r;
            expected = text;
            pattern = provider;
            inDesignField = InDesignSelection.FieldFocused(window);
            if (singleLetter)
                wordLetter = WordSelection.OwnedTextAnchor.Capture(window, text);
        }

        public static SafeEdit InsertionAnchor(IntPtr window)
        {
            if (ExcelCellField.Focused(window))
            {
                var snap = ExcelCellField.Capture(window);
                return snap != null && snap.SelectionStart == snap.SelectionEnd ? ExcelEdit(snap) : null;
            }

            if (InDesignSelection.UseDocumentBridge(window))
                return InDesignInsertion(window);
            try
            {
                if (window != Native.GetForegroundWindow())
                    return null;
                var focus = EmbeddedWebViewFocus.Focused(window);
                if (focus == null)
                    return null;
                object provider;
                if (focus.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                {
                    var text = (TextPattern)provider;
                    var selection = text.GetSelection();
                    if (selection.Length != 1 || selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) != 0)
                        return null;
                    var caret = selection[0].Clone();
                    return new SafeEdit(window, caret, "", text)
                    {
                        focused = focus,
                        capturedCaret = true,
                        caretAtRead = caret.Clone()
                    };
                }

                IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
                if (handle == IntPtr.Zero)
                    return null;
                var kind = new StringBuilder(128);
                Native.GetClassName(handle, kind, 128);
                if (!kind.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) && !kind.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) && !kind.ToString().StartsWith("WindowsForms10.EDIT.", StringComparison.OrdinalIgnoreCase))
                    return null;
                int start, end;
                Native.GetEditSelection(handle, out start, out end);
                if (start != end || start < 0)
                    return null;
                return new SafeEdit(window, null, "")
                {
                    focused = focus,
                    editHandle = handle,
                    editStart = start,
                    editEnd = end,
                    capturedCaret = true,
                    caretAtReadNative = start
                };
            }
            catch
            {
                return null;
            }
        }

        public static SafeEdit ReadSelected(IntPtr window, out bool selectionPresent)
        {
            selectionPresent = false;
            if (ExcelCellField.Focused(window))
            {
                var snap = ExcelCellField.Capture(window);
                if (snap == null)
                {
                    selectionPresent = true;
                    return null;
                }

                selectionPresent = snap.SelectionStart != snap.SelectionEnd;
                return selectionPresent ? ExcelEdit(snap, false, true) : null;
            }

            if (InDesignFindField.Focused(window))
            {
                var snap = InDesignFindField.Capture(window);
                if (snap == null)
                {
                    selectionPresent = true;
                    return null;
                }

                selectionPresent = snap.SelectionStart != snap.SelectionEnd;
                return selectionPresent ? FindEdit(snap, false, true) : null;
            }

            if (InDesignSelection.UseDocumentBridge(window))
            {
                var snap = InDesignSelection.Read(window);
                if (snap == null)
                {
                    selectionPresent = true;
                    return null;
                }

                selectionPresent = snap.SelectionStart != snap.SelectionEnd;
                if (!selectionPresent || !SelectionConversion.ValidText(snap.Text))
                    return null;
                if (snap.Text.Length <= 1024 && !InDesignSelection.Describe(snap))
                    return null;
                return InDesignEdit(snap, false, true);
            }

            try
            {
                if (window != Native.GetForegroundWindow())
                    return null;
                AutomationElement focus = EmbeddedWebViewFocus.Focused(window);
                if (focus == null)
                    return null;
                object provider;
                if (focus.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                {
                    TextPattern pattern = (TextPattern)provider;
                    var selected = pattern.GetSelection();
                    if (selected.Length != 1)
                    {
                        selectionPresent = selected.Length > 1;
                        return null;
                    }

                    var range = selected[0];
                    selectionPresent = range.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) != 0;
                    if (!selectionPresent)
                        return null;
                    if (object.Equals(range.GetAttributeValue(TextPattern.IsReadOnlyAttribute), true))
                        return null;
                    string raw = range.GetText(200001);
                    if (raw.Length > 200000)
                        return null;
                    string text = Lines(raw);
                    if (!SelectionConversion.ValidText(text))
                        return null;
                    return new SafeEdit(window, range.Clone(), text, pattern)
                    {
                        focused = focus,
                        userSelection = true
                    };
                }

                IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
                if (handle == IntPtr.Zero)
                    return null;
                var kind = new StringBuilder(128);
                Native.GetClassName(handle, kind, 128);
                if (!kind.ToString().Equals("Edit", StringComparison.OrdinalIgnoreCase) && !kind.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) && !kind.ToString().StartsWith("WindowsForms10.EDIT.", StringComparison.OrdinalIgnoreCase))
                    return null;
                int start, end;
                Native.GetEditSelection(handle, out start, out end);
                selectionPresent = start != end;
                if (!selectionPresent || start < 0 || end < start || end - start > 200000)
                    return null;
                string textBuffer;
                if (!Native.TryReadEditText(handle, out textBuffer) || end > textBuffer.Length)
                    return null;
                return new SafeEdit(window, null, textBuffer.Substring(start, end - start))
                {
                    focused = focus,
                    editHandle = handle,
                    editStart = start,
                    editEnd = end,
                    userSelection = true
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string ContextThroughWord()
        {
            if (excelCell != null)
                return excelCell.Full.Substring(0, excelCell.End);
            if (findField != null)
                return findField.Full.Substring(0, findField.End);
            if (inDesign != null)
                return inDesign.Left + inDesign.Text;
            try
            {
                if (!SameTarget())
                    return null;
                if (range != null)
                {
                    var nearby = range.Clone();
                    nearby.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -100);
                    string value = nearby.GetText(-1);
                    return value.EndsWith(expected, StringComparison.Ordinal) ? value : null;
                }

                string text = NativeText();
                return editEnd <= text.Length ? text.Substring(Math.Max(0, editStart - 100), editEnd - Math.Max(0, editStart - 100)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string SelectedFont
        {
            get
            {
                if (inDesignField)
                    return null;
                if (inDesign != null)
                    return inDesign.Font.Split('\t')[0];
                try
                {
                    if (range != null)
                        return range.GetAttributeValue(TextPattern.FontNameAttribute) as string;
                    if (editHandle != IntPtr.Zero)
                    {
                        IntPtr handle = Native.SendMessage(editHandle, 0x31, IntPtr.Zero, IntPtr.Zero);
                        if (handle != IntPtr.Zero)
                            using (Font font = Font.FromHfont(handle))
                                return font.Name;
                    }
                }
                catch (Exception)
                {
                }

                return null;
            }
        }

        public bool SelectedMixed
        {
            get
            {
                if (excelCell != null)
                    return false;
                if (inDesignField)
                    return false;
                if (inDesign != null)
                    return inDesign.Mixed;
                try
                {
                    return range != null && range.GetAttributeValue(TextPattern.FontNameAttribute) == TextPattern.MixedAttributeValue;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public string BalaramWordFont { get; private set; }
        public string MixedWordUnicode { get; private set; }

        string MixedBalaramFont(FontEncoding fonts, Core core = null)
        {
            if (!WordPasteOptions.IsWord(Window) || range == null || expected.Length > 160 || expected.Any(char.IsWhiteSpace))
                return null;
            bool[] protectedCharacters = core == null ? null : core.CompoundProtectedMask(expected);
            var texts = new System.Collections.Generic.List<string>();
            var names = new System.Collections.Generic.List<string>();
            var cursor = range.Clone();
            cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
            for (int n = 0; n < 160; n++)
            {
                if (cursor.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) >= 0)
                {
                    string family = MixedWordFonts.BalaramFamily(expected, texts.ToArray(), names.ToArray(), fonts, protectedCharacters);
                    if (family != null)
                    {
                        var decoded = new StringBuilder();
                        int offset = 0;
                        for (int run = 0; run < texts.Count; run++)
                            foreach (char c in texts[run])
                            {
                                bool kept = protectedCharacters != null && protectedCharacters[offset];
                                decoded.Append(!kept && fonts.Resolve(names[run]) == "balaram" ? EncodingMap.FromBalaram(c.ToString()) : c.ToString());
                                offset++;
                            }

                        MixedWordUnicode = decoded.ToString();
                    }

                    return family;
                }

                var piece = cursor.Clone();
                piece.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 1);
                if (piece.CompareEndpoints(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End) > 0)
                    piece.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End);
                string text = Lines(piece.GetText(-1));
                object font = piece.GetAttributeValue(TextPattern.FontNameAttribute);
                if (text.Length == 0 || font == TextPattern.MixedAttributeValue)
                    return null;
                texts.Add(text);
                names.Add(font as string);
                cursor.MoveEndpointByRange(TextPatternRangeEndpoint.Start, piece, TextPatternRangeEndpoint.End);
                cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
            }

            return null;
        }

        public string SelectionEncoding(FontEncoding fonts, EncodingSettings settings, Core core = null)
        {
            if (Numbers.Single(expected) != null && (core == null || !core.HasShortAbbreviation(expected)))
            {
                BalaramWordFont = null;
                MixedWordUnicode = null;
                return "plain";
            }

            if (settings.Mode == "auto" && userSelection && WordPasteOptions.IsWord(Window))
            {
                string nativeFont = WordSelection.SelectedFontName(Window, expected);
                if (nativeFont != null)
                {
                    BalaramWordFont = null;
                    MixedWordUnicode = null;
                    return settings.Resolve(fonts, nativeFont, false);
                }
            }

            BalaramWordFont = null;
            MixedWordUnicode = null;
            if (settings.Mode == "auto" && userSelection && SelectedMixed)
                return "font-auto";
            if (inDesign != null && SelectedMixed)
            {
                if (settings.Mode != "auto")
                    return settings.Resolve(fonts, null, true);
                if (inDesign.Fonts == null)
                    return null;
                var modes = inDesign.Fonts.Select(f => fonts.Resolve(f.Split('\t')[0])).Distinct().ToArray();
                return modes.Length == 1 ? modes[0] : null;
            }

            if (!SelectedMixed)
                return settings.Resolve(fonts, SelectedFont, false);
            try
            {
                if (settings.Mode == "auto" || settings.Mode == "balaram")
                {
                    BalaramWordFont = MixedBalaramFont(fonts, core);
                    if (BalaramWordFont != null)
                        return "balaram";
                }

                if (settings.Mode != "auto")
                    return settings.Resolve(fonts, null, true);
                string mode = null, all = "";
                var cursor = range.Clone();
                cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
                for (int n = 0; n < 1024; n++)
                {
                    if (cursor.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) >= 0)
                        return all == expected ? mode : null;
                    var piece = cursor.Clone();
                    piece.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Format, 1);
                    if (piece.CompareEndpoints(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End) > 0)
                        piece.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End);
                    string text = Lines(piece.GetText(-1));
                    object font = piece.GetAttributeValue(TextPattern.FontNameAttribute);
                    if (text.Length == 0 || font == TextPattern.MixedAttributeValue)
                        return null;
                    string found = fonts.Resolve(font as string);
                    if (found == null || mode != null && mode != found)
                        return null;
                    mode = found;
                    all += text;
                    cursor.MoveEndpointByRange(TextPatternRangeEndpoint.Start, piece, TextPatternRangeEndpoint.End);
                    cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public string FontAutoOutput(Core core, FontEncoding fonts, out ItalicSpan[] italics, bool ctrlExpansion = false)
        {
            italics = null;
            string[] names = null;
            if (inDesign != null)
                names = inDesign.Fonts;
            else
                try
                {
                    var list = new System.Collections.Generic.List<string>();
                    var cursor = range.Clone();
                    cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
                    for (int n = 0; n < 1024; n++)
                    {
                        if (cursor.CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) >= 0)
                            break;
                        var piece = cursor.Clone();
                        piece.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Format, 1);
                        if (piece.CompareEndpoints(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End) > 0)
                            piece.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End);
                        string text = Lines(piece.GetText(-1));
                        object font = piece.GetAttributeValue(TextPattern.FontNameAttribute);
                        if (text.Length == 0 || font == TextPattern.MixedAttributeValue)
                            return expected;
                        list.AddRange(Enumerable.Repeat(font as string, text.Length));
                        cursor.MoveEndpointByRange(TextPatternRangeEndpoint.Start, piece, TextPatternRangeEndpoint.End);
                        cursor.MoveEndpointByRange(TextPatternRangeEndpoint.End, cursor, TextPatternRangeEndpoint.Start);
                    }

                    names = list.ToArray();
                }
                catch
                {
                    return expected;
                }

            if (names == null || names.Length != expected.Length)
                return expected;
            if (ctrlExpansion && Core.IsScriptureReference(expected))
            {
                var encodings = names.Select(name => fonts.Resolve((name ?? "").Split('\t')[0])).Distinct().ToArray();
                if (encodings.Length != 1 || encodings[0] == null)
                    return expected;
                string expanded = core.CtrlExpansion(expected, encodings[0]);
                if (expanded == null)
                    return expected;
                italics = core.CtrlItalics(expected, expanded, encodings[0]);
                return expanded;
            }

            var output = new StringBuilder();
            var common = new StringBuilder();
            var formats = new System.Collections.Generic.List<string>();
            var spans = new System.Collections.Generic.List<ItalicSpan>();
            int at = 0;
            while (at < expected.Length)
            {
                string mode = fonts.Resolve((names[at] ?? "").Split('\t')[0]);
                int end = at + 1;
                while (end < expected.Length && fonts.Resolve((names[end] ?? "").Split('\t')[0]) == mode)
                    end++;
                if (end < expected.Length && char.IsLetter(expected[end - 1]) && char.IsLetter(expected[end]))
                {
                    while (end < expected.Length && char.IsLetter(expected[end]))
                        end++;
                    mode = null;
                }

                string input = expected.Substring(at, end - at), value = mode == null ? input : core.ConvertCopied(input, mode);
                if (mode != null)
                    foreach (var span in core.ControlItalics(null, value, mode, ctrlExpansion && Core.SingleControlWord(expected)))
                        spans.Add(new ItalicSpan(output.Length + span.Start, span.Length, span.Force, span.Italic));
                output.Append(value);
                common.Append(mode == "balaram" ? EncodingMap.FromBalaram(value) : value);
                formats.AddRange(Enumerable.Repeat(mode, value.Length));
                at = end;
            }

            string result = output.ToString();
            if (!formats.Contains(null) && common.Length == result.Length)
            {
                string named = core.ConvertCopied(common.ToString(), "unicode");
                if (named.Length == result.Length)
                {
                    italics = core.ControlItalics(null, named, "unicode", ctrlExpansion && Core.SingleControlWord(expected));
                    var encoded = new StringBuilder();
                    for (int n = 0; n < named.Length; n++)
                        encoded.Append(formats[n] == "balaram" ? EncodingMap.Balaram(named[n].ToString()) : named[n].ToString());
                    return encoded.ToString();
                }
            }

            italics = spans.ToArray();
            return result;
        }

        public bool OwnExistingSelection(Func<bool> context)
        {
            editContext = context;
            return userSelection && ReadyForPaste();
        }

        // A dynamic editor can rebuild its text ranges after a successful input.
        // On timeout, fresh editable-selection evidence may release queued typing
        // without retrying the replacement or trusting a stale range.
        public static bool ReleaseUnverifiedCaret(IntPtr owner, int[] field)
        {
            try
            {
                if (owner != Native.GetForegroundWindow() || !EditGuard.SameFocus(field, Native.FocusIdentity()))
                    return false;
                var focused = EmbeddedWebViewFocus.Focused(owner);
                object provider;
                if (focused == null || !focused.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                    return false;
                var pattern = (TextPattern)provider;
                var selected = pattern.GetSelection();
                if (selected.Length != 1 || object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), true))
                    return false;
                var end = selected[0].Clone();
                end.MoveEndpointByRange(TextPatternRangeEndpoint.Start, end, TextPatternRangeEndpoint.End);
                if (!EditGuard.SameFocus(field, Native.FocusIdentity()))
                    return false;
                end.Select();
                var now = pattern.GetSelection();
                return now.Length == 1 && now[0].CompareEndpoints(TextPatternRangeEndpoint.Start, now[0], TextPatternRangeEndpoint.End) == 0 && now[0].CompareEndpoints(TextPatternRangeEndpoint.Start, end, TextPatternRangeEndpoint.End) == 0 && EditGuard.SameFocus(field, Native.FocusIdentity());
            }
            catch
            {
                return false;
            }
        }

        static string Lines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        public static SafeEdit BehindCaret(IntPtr window, string expected, bool singleLetter = false, bool allowCase = false, bool arbitraryRange = false)
        {
            if (ExcelCellField.Focused(window))
            {
                var snap = ExcelCellField.Capture(window);
                if (snap == null || snap.SelectionStart != snap.SelectionEnd || expected == null || snap.SelectionStart < expected.Length)
                    return null;
                int start = snap.SelectionStart - expected.Length;
                string observed = snap.Full.Substring(start, expected.Length);
                if (observed != expected && !(allowCase && EditGuard.CaseEquivalent(expected, observed)))
                    return null;
                if (!arbitraryRange && !singleLetter && (!EditGuard.LeftBoundary(snap.Full, start, observed) || !EditGuard.RightBoundary(snap.Full, snap.SelectionEnd, observed)))
                    return null;
                snap.Start = start;
                snap.End = snap.SelectionEnd;
                return ExcelEdit(snap, singleLetter);
            }

            if (InDesignFindField.Focused(window))
            {
                var snap = InDesignFindField.Capture(window);
                if (snap == null || snap.SelectionStart != snap.SelectionEnd || expected == null || snap.SelectionStart < expected.Length)
                    return null;
                string observed = snap.Full.Substring(snap.SelectionStart - expected.Length, expected.Length);
                if (observed != expected && !(allowCase && EditGuard.CaseEquivalent(expected, observed)))
                    return null;
                snap.Start = snap.SelectionStart - expected.Length;
                snap.End = snap.SelectionStart;
                if (!InDesignFindSyntax.IsLiteralRange(snap.Full, snap.Start, snap.End))
                    return null;
                return FindEdit(snap, singleLetter);
            }

            if (InDesignSelection.UseDocumentBridge(window))
            {
                var snap = InDesignSelection.Read(window);
                if (snap == null || snap.SelectionStart != snap.SelectionEnd || expected == null || expected.Length > snap.Left.Length)
                    return null;
                string observed = snap.Left.Substring(snap.Left.Length - expected.Length);
                if (observed != expected && !(allowCase && !singleLetter && EditGuard.CaseEquivalent(expected, observed)))
                    return null;
                if (singleLetter && !SingleLetters.Supported(observed))
                    return null;
                if (!arbitraryRange && !singleLetter && (!EditGuard.LeftBoundary(snap.Left, snap.Left.Length - observed.Length, observed) || !EditGuard.RightBoundary(observed + snap.Right, observed.Length, observed)))
                    return null;
                snap.Text = observed;
                snap.Start = snap.SelectionStart - observed.Length;
                snap.End = snap.SelectionEnd;
                snap.Offset = observed.Length;
                if (observed.Length <= 1024 && !InDesignSelection.Describe(snap))
                    return null;
                return InDesignEdit(snap, singleLetter);
            }

            if (singleLetter && !SingleLetters.Supported(expected))
                return null;
            if (window != Native.GetForegroundWindow())
                return null;
            try
            {
                AutomationElement focus = EmbeddedWebViewFocus.Focused(window);
                if (focus == null)
                    return null;
                object obj;
                if (!focus.TryGetCurrentPattern(TextPattern.Pattern, out obj))
                    return NativeEdit(window, focus, expected, singleLetter, allowCase);
                TextPattern pattern = (TextPattern)obj;
                TextPatternRange[] selected = pattern.GetSelection();
                if (selected.Length != 1)
                    return null;
                TextPatternRange end = selected[0].Clone();
                if (end.CompareEndpoints(TextPatternRangeEndpoint.Start, end, TextPatternRangeEndpoint.End) != 0)
                    return null;
                string text = Lines(expected);
                TextPatternRange start = end.Clone();
                // Text providers differ in CRLF units. Search a bounded nearby range and verify the exact text.
                start.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -text.Length);
                string observed = Lines(start.GetText(-1));
                if (observed != text && !(allowCase && !singleLetter && EditGuard.CaseEquivalent(text, observed)))
                    return null;
                text = observed;
                if (!arbitraryRange && !Boundary(start, text, singleLetter))
                    return null;
                var edit = new SafeEdit(window, start, text, pattern, singleLetter)
                {
                    focused = focus,
                    userSelection = arbitraryRange
                };
                if (!singleLetter && !arbitraryRange)
                    edit.wordLetter = WordSelection.OwnedTextAnchor.Capture(window, text, false);
                return edit;
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (COMException)
            {
                return null;
            }
        }

        static SafeEdit NativeEdit(IntPtr window, AutomationElement focus, string expected, bool singleLetter, bool allowCase)
        {
            IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
            if (handle == IntPtr.Zero)
                return null;
            StringBuilder name = new StringBuilder(128);
            Native.GetClassName(handle, name, 128);
            string cls = name.ToString();
            expected = Native.EditTextCoordinates(handle, expected);
            if (!cls.Equals("Edit", StringComparison.OrdinalIgnoreCase) && !cls.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) && !cls.Contains("EDIT"))
                return null;
            int start, end;
            Native.GetEditSelection(handle, out start, out end);
            if (start != end || start < expected.Length)
                return null;
            string text;
            if (!Native.TryReadEditText(handle, out text))
                return null;
            if (allowCase && !singleLetter && start <= text.Length)
            {
                string observed = text.Substring(start - expected.Length, expected.Length);
                if (EditGuard.CaseEquivalent(expected, observed))
                    expected = observed;
            }

            int begin;
            if (!(singleLetter ? LetterEditGuard.Locate(text, start, end, expected, out begin) : EditGuard.Locate(text, start, end, expected, out begin)))
                return null;
            return new SafeEdit(window, null, expected, null, singleLetter)
            {
                focused = focus,
                editHandle = handle,
                editStart = begin,
                editEnd = end
            };
        }

        bool SameFocus()
        {
            AutomationElement current = EmbeddedWebViewFocus.Focused(Window);
            if (current == null || focused == null)
                return false;
            return EditGuard.SameFocus(FocusIdentity(), current.GetRuntimeId());
        }

        static bool Boundary(TextPatternRange range, string expected, bool singleLetter = false)
        {
            if (singleLetter)
                return SingleLetters.Supported(expected);
            TextPatternRange before = range.Clone();
            before.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, -1);
            string prefix = Lines(before.GetText(-1));
            if (!EditGuard.LeftBoundary(prefix, prefix.Length - expected.Length, expected))
                return false;
            TextPatternRange after = range.Clone();
            after.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 1);
            string tail = Lines(after.GetText(-1));
            return EditGuard.RightBoundary(tail, expected.Length, expected);
        }

        Func<bool> editContext;
        bool SameTarget()
        {
            return Window == Native.GetForegroundWindow() && (excelCell != null ? excelCell.Current : inDesign != null || SameFocus());
        }

        string NativeText()
        {
            string text;
            if (!Native.TryReadEditText(editHandle, out text))
                throw new InvalidOperationException("Native edit text unavailable");
            return text;
        }

        public bool AtOwnedCaret()
        {
            if (excelCell != null)
                return (editContext == null || editContext()) && excelCell.Matches(true);
            if (findField != null)
                return editContext == null ? findField.Matches(true) : editContext() && findField.Matches(true);
            if (wordLetter != null)
                return SameTarget() && wordLetter.AtCaret();
            if (inDesign != null)
                return inDesign.Matches(true);
            try
            {
                if (!SameTarget())
                {
                    Detail("target or focus changed");
                    return false;
                }

                if (capturedCaret)
                {
                    if (editHandle != IntPtr.Zero)
                    {
                        int start, end;
                        Native.GetEditSelection(editHandle, out start, out end);
                        string text = NativeText();
                        return start == end && start == caretAtReadNative && editStart >= 0 && editEnd <= text.Length && text.Substring(editStart, editEnd - editStart) == expected;
                    }

                    var selection = pattern.GetSelection();
                    return selection.Length == 1 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) == 0 && selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, caretAtRead, TextPatternRangeEndpoint.Start) == 0 && range.GetText(-1) == expected;
                }

                if (editHandle != IntPtr.Zero)
                {
                    int start, end;
                    Native.GetEditSelection(editHandle, out start, out end);
                    int begin;
                    return start == editEnd && end == editEnd && (SingleLetter ? LetterEditGuard.Locate(NativeText(), start, end, expected, out begin) : EditGuard.Locate(NativeText(), start, end, expected, out begin)) && begin == editStart;
                }

                TextPatternRange[] current = pattern.GetSelection();
                return current.Length == 1 && current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End) == 0 && range.CompareEndpoints(TextPatternRangeEndpoint.End, current[0], TextPatternRangeEndpoint.Start) == 0 && Lines(range.GetText(-1)) == expected && Boundary(range, expected, SingleLetter);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ExactSelection()
        {
            if (excelCell != null)
                return (editContext == null || editContext()) && excelCell.Matches(false);
            if (findField != null)
                return (editContext == null || editContext()) && findField.Matches(false);
            if (wordLetter != null)
                return SameTarget() && wordLetter.Selected();
            if (inDesign != null)
                return inDesign.Matches(false);
            try
            {
                if (!SameTarget())
                {
                    Detail("target or focus changed");
                    return false;
                }

                if (editHandle != IntPtr.Zero)
                {
                    int start, end;
                    Native.GetEditSelection(editHandle, out start, out end);
                    string text = NativeText();
                    return start == editStart && end == editEnd && end <= text.Length && text.Substring(start, end - start) == expected && (userSelection || capturedCaret || (SingleLetter ? LetterEditGuard.Selected(text, start, end, expected, editStart, editEnd) : EditGuard.LeftBoundary(text, start, expected) && EditGuard.RightBoundary(text, end, expected)));
                }

                TextPatternRange[] current = pattern.GetSelection();
                if (current.Length != 1)
                {
                    Detail("selection count=" + current.Length);
                    return false;
                }

                bool expanded = current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End) != 0, startSame = range.CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.Start) == 0, endSame = range.CompareEndpoints(TextPatternRangeEndpoint.End, current[0], TextPatternRangeEndpoint.End) == 0, textSame = Lines(current[0].GetText(-1)) == expected, rangeSame = Lines(range.GetText(-1)) == expected, boundary = userSelection || capturedCaret || Boundary(range, expected, SingleLetter);
                if (userSelection || capturedCaret)
                {
                    bool owned = SelectionCommit.OwnedRange(expanded, startSame, endSame, textSame, rangeSame, true, false, null);
                    if (!owned && IsMarkdownSource)
                        Detail("markdown selection expanded=" + expanded + " start=" + startSame + " end=" + endSame + " text=" + textSame + " range=" + rangeSame + " expectedCharacters=" + expected.Length + " observedCharacters=" + Lines(current[0].GetText(-1)).Length);
                    return owned;
                }

                string gap = null;
                bool word = WordPasteOptions.IsWord(Window);
                if (!endSame && word && expanded && startSame && textSame && rangeSame && boundary)
                {
                    var between = range.Clone();
                    int order = range.CompareEndpoints(TextPatternRangeEndpoint.End, current[0], TextPatternRangeEndpoint.End);
                    if (order < 0)
                    {
                        between.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End);
                        between.MoveEndpointByRange(TextPatternRangeEndpoint.End, current[0], TextPatternRangeEndpoint.End);
                    }
                    else
                    {
                        between.MoveEndpointByRange(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End);
                        between.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End);
                    }

                    gap = between.GetText(-1);
                    boundary = Boundary(current[0], expected, SingleLetter);
                    Detail("Word equivalent-end candidate gap-length=" + (gap == null ? -1 : gap.Length) + " boundary=" + boundary);
                }

                bool exact = SelectionCommit.OwnedRange(expanded, startSame, endSame, textSame, rangeSame, boundary, word, gap);
                if (!exact)
                    Detail("selection expanded=" + expanded + " start=" + startSame + " end=" + endSame + " text=" + textSame + " range=" + rangeSame + " boundary=" + boundary);
                return exact;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RequestSelection()
        {
            if (excelCell != null)
            {
                excelCell.Select();
                return;
            }

            if (findField != null)
            {
                findField.Select();
                return;
            }

            if (wordLetter != null)
            {
                if (SameTarget())
                    wordLetter.Select();
                return;
            }

            if (inDesign != null)
            {
                inDesign.Select();
                return;
            }

            try
            {
                if (!AtOwnedCaret())
                    return;
                if (editHandle != IntPtr.Zero)
                    Native.SelectEditRange(editHandle, editStart, editEnd);
                else
                    range.Select();
            }
            catch (Exception)
            {
            }
        }

        bool AtWordEnd()
        {
            try
            {
                if (!SameTarget())
                    return false;
                if (editHandle != IntPtr.Zero)
                {
                    int start, end;
                    Native.GetEditSelection(editHandle, out start, out end);
                    string text = NativeText();
                    return start == end && start == editEnd && editEnd <= text.Length && text.Substring(editStart, editEnd - editStart) == expected;
                }

                var current = pattern.GetSelection();
                return current.Length == 1 && current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End) == 0 && current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End) == 0 && range.GetText(-1) == expected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RequestKeyboardSelection()
        {
            if (excelCell != null)
                return excelCell.Select();
            if (findField != null)
                return findField.Select();
            if (wordLetter != null)
                return SameTarget() && wordLetter.Select();
            if (inDesign != null)
                return inDesign.Select();
            if (capturedCaret && !SingleLetter)
            {
                int wordCount = CaretWords.NavigationCount(expected);
                if (wordCount == 0 || !AtOwnedCaret() || editContext == null || !editContext() || !Native.MoveCaretCharacters(expected.Length - CaretOffset))
                    return false;
                for (int i = 0; i < 15; i++)
                {
                    if (!editContext())
                        return false;
                    if (AtWordEnd())
                        return Native.SelectPrevious(wordCount);
                    Thread.Sleep(10);
                }

                return false;
            }

            int count = SingleLetter && SingleLetters.Supported(expected) ? 1 : SelectionCommit.KeyboardCount(expected);
            return count > 0 && AtOwnedCaret() && Native.SelectPrevious(count);
        }

        public bool RestoreCaret()
        {
            if (excelCell != null)
                return excelCell.Restore();
            if (findField != null)
                return findField.Restore();
            if (wordLetter != null)
                return SameTarget() && wordLetter.Restore();
            if (inDesign != null)
                return inDesign.RestoreCaret();
            try
            {
                if (!SameTarget())
                {
                    Detail("target or focus changed");
                    return false;
                }

                if (AtOwnedCaret())
                    return true;
                if (editHandle != IntPtr.Zero)
                {
                    int start, end;
                    Native.GetEditSelection(editHandle, out start, out end);
                    string text = NativeText();
                    if (start < editStart || end > editEnd || text.Length < editEnd || text.Substring(editStart, editEnd - editStart) != expected)
                        return false;
                    int restored = capturedCaret ? caretAtReadNative : editEnd;
                    Native.SelectEditRange(editHandle, restored, restored);
                    return AtOwnedCaret();
                }

                if (Lines(range.GetText(-1)) != expected || (!capturedCaret && !Boundary(range, expected, SingleLetter)))
                    return false;
                TextPatternRange[] current = pattern.GetSelection();
                if (current.Length != 1)
                    return false;
                bool exact = ExactSelection();
                bool atStart = current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End) == 0 && range.CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.Start) == 0;
                bool within = false;
                if (capturedCaret && current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, current[0], TextPatternRangeEndpoint.End) == 0)
                    within = current[0].CompareEndpoints(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.Start) >= 0 && current[0].CompareEndpoints(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.End) <= 0;
                if (!exact && !atStart && !within)
                    return false;
                TextPatternRange recoveryEnd = capturedCaret ? caretAtRead.Clone() : range.Clone();
                if (!capturedCaret)
                    recoveryEnd.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End);
                recoveryEnd.Select();
                for (int i = 0; i < 10; i++)
                {
                    if (editContext != null && !editContext())
                        return false;
                    if (AtOwnedCaret())
                        return true;
                    Thread.Sleep(10);
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RecoverAutomaticCaret(string output)
        {
            try
            {
                if (!SameTarget() || output == null || stableStart == null)
                    return false;
                if (editHandle != IntPtr.Zero)
                {
                    string text = NativeText();
                    if (editStart < 0 || editStart + output.Length > text.Length || !stableStart.Matches(text.Substring(0, editStart)) || text.Substring(editStart, output.Length) != output)
                        return false;
                    Native.SelectEditRange(editHandle, editStart + output.Length, editStart + output.Length);
                    return true;
                }

                if (range == null || pattern == null)
                    return false;
                var search = pattern.DocumentRange.Clone();
                for (int n = 0; n < 128; n++)
                {
                    var found = search.FindText(output, false, false);
                    if (found == null)
                        break;
                    var candidate = new SafeEdit(Window, found, output, pattern)
                    {
                        focused = focused
                    };
                    if (stableStart.Matches(candidate.StartPrefix()))
                    {
                        found.MoveEndpointByRange(TextPatternRangeEndpoint.Start, found, TextPatternRangeEndpoint.End);
                        found.Select();
                        return true;
                    }

                    search.MoveEndpointByRange(TextPatternRangeEndpoint.Start, found, TextPatternRangeEndpoint.End);
                }

                if (!stableStart.Matches(StartPrefix()))
                    return false;
                var target = range.Clone();
                target.MoveEndpointByRange(TextPatternRangeEndpoint.End, target, TextPatternRangeEndpoint.Start);
                for (int n = 0; n <= output.Length + 2; n++)
                {
                    string found = Lines(target.GetText(-1));
                    if (found == output)
                    {
                        target.MoveEndpointByRange(TextPatternRangeEndpoint.Start, target, TextPatternRangeEndpoint.End);
                        target.Select();
                        return true;
                    }

                    if (found.Length > output.Length || target.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 1) != 1)
                        return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Only a verified owned selection can be collapsed for queued typing.
        public bool ResumeManual(string output, bool selected, bool typing)
        {
            if (excelCell != null)
                return excelCell.Resume(output, selected, typing);
            Func<bool> old = editContext;
            editContext = () => Window == Native.GetForegroundWindow() && SameTarget();
            try
            {
                if (!editContext())
                    return false;
                if (output == null)
                {
                    if (userSelection)
                        return typing ? ResumeManual(expected, true, true) : ExactSelection();
                    return RestoreCaret();
                }

                if (selected)
                {
                    if (!typing)
                        return ExactSelection();
                    if (!ExactSelection())
                        return VerifyReplacementCaret(output);
                    if (findField != null)
                        return findField.Position(output, output.Length);
                    if (inDesign != null)
                        return inDesign.CollapseSelected();
                    if (editHandle != IntPtr.Zero)
                    {
                        Native.SelectEditRange(editHandle, editEnd, editEnd);
                        return AtWordEnd();
                    }

                    return VerifyReplacementCaret(output);
                }

                var caret = ReadCaret(Window, SingleLetter);
                if (caret != null && caret.Edit != null && caret.Text == output && SameStart(caret.Edit) && caret.Edit.AtOwnedCaret())
                    return true;
                var behind = BehindCaret(Window, output, SingleLetter, false, true);
                if (behind != null && SameStart(behind) && behind.AtOwnedCaret())
                    return true;
                if (findField != null)
                    return findField.Position(output, output.Length);
                if (inDesign != null)
                    return inDesign.Position(output, output.Length);
                return VerifyReplacementCaret(output);
            }
            catch
            {
                return false;
            }
            finally
            {
                editContext = old;
            }
        }

        public bool SameLetterLocation(SafeEdit other)
        {
            if (excelCell != null)
                return other != null && excelCell.SameStart(other.excelCell) && excelCell.End == other.excelCell.End;
            if (findField != null)
                return other != null && findField.SameStart(other.findField) && findField.End == other.findField.End;
            if (inDesign != null)
                return other != null && inDesign.SameStart(other.inDesign) && inDesign.End == other.inDesign.End;
            try
            {
                if (other == null || Window != other.Window || !EditGuard.SameFocus(FocusIdentity(), other.FocusIdentity()))
                    return false;
                if (editHandle != IntPtr.Zero || other.editHandle != IntPtr.Zero)
                    return editHandle == other.editHandle && editEnd == other.editEnd;
                return range.CompareEndpoints(TextPatternRangeEndpoint.End, other.range, TextPatternRangeEndpoint.End) == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        string StartPrefix()
        {
            try
            {
                if (pattern == null || range == null)
                    return null;
                var prefix = pattern.DocumentRange.Clone();
                prefix.MoveEndpointByRange(TextPatternRangeEndpoint.End, range, TextPatternRangeEndpoint.Start);
                string text = Lines(prefix.GetText(200001));
                return text.Length <= 200000 ? text : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string OccurrenceKey()
        {
            if (excelCell != null)
                return excelCell.Key;
            if (findField != null)
                return findField.Key;
            if (inDesign != null)
                return inDesign.Key;
            try
            {
                string field = string.Join(",", focused.GetRuntimeId());
                if (editHandle != IntPtr.Zero)
                    return Window + ":" + field + ":" + editHandle + ":" + editStart;
                string prefix = StartPrefix();
                return prefix == null ? null : Window + ":" + field + ":" + new SelectionAnchor(prefix).Key;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void CaptureStableStart()
        {
            if (excelCell != null)
                return;
            try
            {
                FocusIdentity();
            }
            catch
            {
            }

            if (inDesign != null)
                return;
            if (editHandle != IntPtr.Zero)
            {
                string text = NativeText();
                if (editStart >= 0 && editStart <= text.Length)
                    stableStart = new SelectionAnchor(text.Substring(0, editStart));
                return;
            }

            string prefix = StartPrefix();
            if (prefix != null)
                stableStart = new SelectionAnchor(prefix);
        }

        public bool SameStart(SafeEdit other)
        {
            if (excelCell != null)
                return other != null && excelCell.SameStart(other.excelCell);
            if (findField != null)
                return other != null && findField.SameStart(other.findField);
            if (inDesign != null)
                return other != null && inDesign.SameStart(other.inDesign);
            try
            {
                if (other == null || Window != other.Window || !EditGuard.SameFocus(FocusIdentity(), other.FocusIdentity()))
                    return false;
                if (editHandle != IntPtr.Zero || other.editHandle != IntPtr.Zero)
                    return editHandle == other.editHandle && editStart == other.editStart;
                if (stableStart != null)
                    return stableStart.Matches(other.StartPrefix());
                if (other.stableStart != null)
                    return other.stableStart.Matches(StartPrefix());
                return range.CompareEndpoints(TextPatternRangeEndpoint.Start, other.range, TextPatternRangeEndpoint.Start) == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool PositionAfterReplacement(string replacement, int desiredOffset, Func<bool> context)
        {
            if (excelCell != null)
                return context() && excelCell.Position(replacement, desiredOffset);
            if (findField != null)
                return context() && findField.Position(replacement, desiredOffset);
            if (inDesign != null)
                return context() && inDesign.Position(replacement, desiredOffset);
            if (desiredOffset == replacement.Length)
                return context();
            for (int i = 0; i < 25; i++)
            {
                if (!context() || !SameTarget())
                    return false;
                var current = ReadCaret(Window, false, !CompoundForms.Pattern.IsMatch(replacement));
                if (current != null && current.Text == replacement && current.Edit != null && SameStart(current.Edit))
                {
                    var updated = current.Edit;
                    string tail = desiredOffset > replacement.Length ? caretTail : "";
                    if (desiredOffset > replacement.Length && tail.Length != desiredOffset - replacement.Length)
                        return false;
                    try
                    {
                        if (updated.editHandle != IntPtr.Zero)
                        {
                            int start, end;
                            Native.GetEditSelection(updated.editHandle, out start, out end);
                            string text = updated.NativeText();
                            int target = updated.editStart + desiredOffset;
                            if (start != end || start != updated.editEnd || target < updated.editStart || target > text.Length || updated.editEnd + tail.Length > text.Length || text.Substring(updated.editEnd, tail.Length) != tail || !context())
                                return false;
                            Native.SelectEditRange(updated.editHandle, target, target);
                            Native.GetEditSelection(updated.editHandle, out start, out end);
                            return start == target && end == target && context();
                        }

                        var selected = updated.pattern.GetSelection();
                        if (selected.Length != 1 || selected[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End) != 0)
                            return false;
                        var verify = updated.range.Clone();
                        verify.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, tail.Length);
                        if (verify.GetText(-1) != replacement + tail || !context())
                            return false;
                        var desired = selected[0].Clone();
                        int delta = desiredOffset - replacement.Length;
                        if (delta < 0)
                        {
                            if (desired.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, delta) != delta)
                                return false;
                            desired.MoveEndpointByRange(TextPatternRangeEndpoint.End, desired, TextPatternRangeEndpoint.Start);
                        }
                        else
                        {
                            if (desired.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, delta) != delta)
                                return false;
                            desired.MoveEndpointByRange(TextPatternRangeEndpoint.Start, desired, TextPatternRangeEndpoint.End);
                        }

                        if (desired.CompareEndpoints(TextPatternRangeEndpoint.Start, desired, TextPatternRangeEndpoint.End) != 0 || !context())
                            return false;
                        desired.Select();
                        for (int wait = 0; wait < 5; wait++)
                        {
                            if (!context())
                                return false;
                            var now = updated.pattern.GetSelection();
                            if (now.Length == 1 && now[0].CompareEndpoints(TextPatternRangeEndpoint.Start, now[0], TextPatternRangeEndpoint.End) == 0 && now[0].CompareEndpoints(TextPatternRangeEndpoint.Start, desired, TextPatternRangeEndpoint.Start) == 0)
                                return true;
                            Thread.Sleep(10);
                        }

                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }

                Thread.Sleep(20);
            }

            return false;
        }

        internal bool VerifyReplacementCaret(string output)
        {
            try
            {
                var owned = ReplacementAtStableStart(output);
                if (owned == null)
                    return false;
                if (owned.editHandle != IntPtr.Zero)
                {
                    Native.SelectEditRange(owned.editHandle, owned.editEnd, owned.editEnd);
                    return owned.AtWordEnd();
                }

                if (ReplacementCaretMatches(owned, output))
                    return true;
                var desired = owned.range.Clone();
                desired.MoveEndpointByRange(TextPatternRangeEndpoint.Start, desired, TextPatternRangeEndpoint.End);
                desired.Select();
                for (int n = 0; n < 10; n++)
                {
                    if (!owned.SameTarget())
                        return false;
                    if (ReplacementCaretMatches(owned, output))
                        return true;
                    Thread.Sleep(5);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        static bool ReplacementCaretMatches(SafeEdit owned, string output)
        {
            try
            {
                if (!owned.SameTarget())
                    return false;
                var selected = owned.pattern.GetSelection();
                if (selected.Length != 1 || selected[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End) != 0 || owned.range.CompareEndpoints(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.Start) > 0)
                    return false;
                var before = owned.range.Clone();
                before.MoveEndpointByRange(TextPatternRangeEndpoint.End, selected[0], TextPatternRangeEndpoint.Start);
                var after = owned.range.Clone();
                after.MoveEndpointByRange(TextPatternRangeEndpoint.Start, selected[0], TextPatternRangeEndpoint.End);
                bool matches = SelectionConversion.PastedCaretMatches(output, before.GetText(400001), after.GetText(33));
                if (matches)
                    Detail("replacement caret verified by complete preceding content");
                return matches;
            }
            catch
            {
                return false;
            }
        }

        SafeEdit ReplacementAtStableStart(string output)
        {
            try
            {
                if (!SameTarget() || stableStart == null)
                {
                    Detail("replacement anchor unavailable target=" + SameTarget() + " prefix=" + (stableStart != null));
                    return null;
                }

                if (editHandle != IntPtr.Zero)
                {
                    string text = NativeText();
                    if (editStart < 0 || editStart + output.Length > text.Length || !stableStart.Matches(text.Substring(0, editStart)) || text.Substring(editStart, output.Length) != output)
                        return null;
                    return new SafeEdit(Window, null, output)
                    {
                        focused = focused,
                        editHandle = editHandle,
                        editStart = editStart,
                        editEnd = editStart + output.Length,
                        userSelection = true
                    };
                }

                if (pattern == null)
                    return null;
                var search = pattern.DocumentRange.Clone();
                for (int n = 0; n < 128; n++)
                {
                    var found = search.FindText(output, false, false);
                    if (found == null)
                        break;
                    var candidate = new SafeEdit(Window, found, output, pattern)
                    {
                        focused = focused,
                        userSelection = true
                    };
                    if (stableStart.Matches(candidate.StartPrefix()))
                        return candidate;
                    if (found.CompareEndpoints(TextPatternRangeEndpoint.End, search, TextPatternRangeEndpoint.Start) <= 0)
                        return null;
                    search.MoveEndpointByRange(TextPatternRangeEndpoint.Start, found, TextPatternRangeEndpoint.End);
                }

                // Some browser providers cannot FindText across paragraph boundaries.
                // Find the end lines separately, then prove the complete intervening
                // text and original prefix. Neither an end-line match nor offsets alone
                // authorize caret movement or queued input replay.
                string normalized = Lines(output);
                int firstBreak = normalized.IndexOf('\n'), lastBreak = normalized.LastIndexOf('\n');
                if (firstBreak <= 0 || lastBreak == normalized.Length - 1 || normalized.Length > 200000)
                    return null;
                string first = normalized.Substring(0, firstBreak), last = normalized.Substring(lastBreak + 1);
                search = pattern.DocumentRange.Clone();
                for (int n = 0; n < 128; n++)
                {
                    var head = search.FindText(first, false, false);
                    if (head == null)
                    {
                        Detail("multiline replacement first line unavailable");
                        return null;
                    }

                    var anchor = new SafeEdit(Window, head, first, pattern)
                    {
                        focused = focused
                    };
                    if (stableStart.Matches(anchor.StartPrefix()))
                    {
                        var endings = pattern.DocumentRange.Clone();
                        endings.MoveEndpointByRange(TextPatternRangeEndpoint.Start, head, TextPatternRangeEndpoint.End);
                        for (int end = 0; end < 128; end++)
                        {
                            var tail = endings.FindText(last, false, false);
                            if (tail == null)
                            {
                                Detail("multiline replacement last line unavailable");
                                return null;
                            }

                            var complete = head.Clone();
                            complete.MoveEndpointByRange(TextPatternRangeEndpoint.End, tail, TextPatternRangeEndpoint.End);
                            string text = Lines(complete.GetText(normalized.Length * 2 + 1));
                            if (SelectionConversion.PastedTextMatches(normalized, text))
                            {
                                Detail("multiline replacement verified by end-line range and complete content");
                                return new SafeEdit(Window, complete, text, pattern)
                                {
                                    focused = focused,
                                    userSelection = true
                                };
                            }

                            Detail("multiline replacement differs expectedCharacters=" + normalized.Length + " observedCharacters=" + text.Length);
                            if (text.Length > normalized.Length || tail.CompareEndpoints(TextPatternRangeEndpoint.End, endings, TextPatternRangeEndpoint.Start) <= 0)
                                return null;
                            endings.MoveEndpointByRange(TextPatternRangeEndpoint.Start, tail, TextPatternRangeEndpoint.End);
                        }

                        return null;
                    }

                    if (head.CompareEndpoints(TextPatternRangeEndpoint.End, search, TextPatternRangeEndpoint.Start) <= 0)
                        return null;
                    search.MoveEndpointByRange(TextPatternRangeEndpoint.Start, head, TextPatternRangeEndpoint.End);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        internal CaretTarget ReadReplacementCaret(string output)
        {
            try
            {
                // Rich paste can rebuild inline ranges without moving the real caret.
                // Reuse only the previous conversion's exact field, original prefix,
                // and committed spelling; do not infer an editable word from neighbors.
                var located = ReplacementAtStableStart(output);
                if (located == null || located.pattern == null)
                    return null;
                var selection = located.pattern.GetSelection();
                if (selection.Length != 1 || selection[0].CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End) != 0 || object.Equals(selection[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), true) || located.range.CompareEndpoints(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.Start) > 0)
                    return null;
                var before = located.range.Clone();
                before.MoveEndpointByRange(TextPatternRangeEndpoint.End, selection[0], TextPatternRangeEndpoint.Start);
                var after = located.range.Clone();
                after.MoveEndpointByRange(TextPatternRangeEndpoint.Start, selection[0], TextPatternRangeEndpoint.End);
                string left = before.GetText(161), right = after.GetText(161);
                if (!CaretWords.RangeAtCaret(output, left.Length, "", left, right))
                    return null;
                object font = located.range.GetAttributeValue(TextPattern.FontNameAttribute);
                located.userSelection = false;
                located.capturedCaret = true;
                located.caretAtRead = selection[0].Clone();
                located.CaretOffset = left.Length;
                located.stableStart = stableStart;
                return new CaretTarget(output, font as string, font == TextPattern.MixedAttributeValue, located);
            }
            catch
            {
                return null;
            }
        }

        public SafeEdit RetainReplacement(string output, Func<bool> context)
        {
            for (int n = 0; n < 25; n++)
            {
                if (!context())
                    return null;
                bool present;
                var selected = ReadSelected(Window, out present);
                if (present)
                {
                    if (RepresentsOutput(selected, output))
                        return selected;
                    Thread.Sleep(10);
                    continue;
                }

                var current = BehindCaret(Window, output, false, false, true) ?? BehindCaret(Window, DisplayedOutput(output), false, false, true);
                if (n == 0)
                    Detail("retention candidate=" + (current != null) + " same-start=" + (current != null && SameStart(current)) + " caret=" + (current != null && current.AtOwnedCaret()));
                if (current != null && SameStart(current) && current.Select(context) && current.ExactSelection())
                    return current;
                // A browser can report stale caret offsets after paste or DOM redraw.
                // Re-find only the committed output at its original verified prefix.
                var located = ReplacementAtStableStart(output);
                if (located != null && context())
                {
                    if (located.editHandle != IntPtr.Zero)
                        Native.SelectEditRange(located.editHandle, located.editStart, located.editEnd);
                    else
                        located.range.Select();
                    if (context() && located.ExactSelection())
                        return located;
                }

                Thread.Sleep(20);
            }

            return null;
        }

        public void Cancel()
        {
            if (editContext != null && editContext())
                RestoreCaret();
        }

        public bool Select(Func<bool> context, bool keyboardFirst = false)
        {
            editContext = context;
            if (excelCell != null)
                return context() && (excelCell.Matches(false) || excelCell.Select()) && context();
            if (findField != null)
                return context() && (findField.Matches(false) || findField.Select()) && context();
            if (inDesign != null)
                return context() && (inDesign.Matches(false) || inDesign.Select()) && context();
            return SelectionCommit.Prepare(this, context, null, keyboardFirst && (SingleLetter || CaretWords.NavigationCount(expected) > 0));
        }

        public bool ReadyForPaste()
        {
            if (excelCell != null)
                return editContext != null && editContext() && excelCell.Matches(false);
            if (findField != null)
                return editContext != null && editContext() && findField.Matches(false);
            if (inDesign != null)
                return editContext != null && editContext() && inDesign.Matches(false);
            return editContext != null && SelectionCommit.CanPaste(this, editContext);
        }
    }

    public static class WordPasteOptions
    {
        public static bool IsWord(IntPtr owner)
        {
            var name = new StringBuilder(64);
            Native.GetClassName(owner, name, name.Capacity);
            return name.ToString() == "OpusApp";
        }

        public static IntPtr Owner(IntPtr window)
        {
            IntPtr root = Native.GetAncestor(window, 3);
            return root != IntPtr.Zero && IsWord(root) ? root : window;
        }

        static bool Owned(IntPtr owner, AutomationElement focus)
        {
            if (owner != Owner(Native.GetForegroundWindow()) || focus == null)
                return false;
            uint process;
            Native.GetWindowThreadProcessId(owner, out process);
            var name = new StringBuilder(64);
            Native.GetClassName(owner, name, name.Capacity);
            return WordPasteGuard.Owned(name.ToString(), process, focus.Current.ProcessId);
        }

        public static bool DocumentFocused(IntPtr owner)
        {
            try
            {
                if (!IsWord(owner))
                    return false;
                bool modern;
                var focus = WordCommentFocus.Focused(owner, out modern);
                if (modern)
                    return WordCommentFocus.Editable(focus);
                if (!Owned(owner, focus))
                    return false;
                int process = focus.Current.ProcessId;
                // Word may focus an Edit/Text child instead of the Body document itself.
                for (int i = 0; i < 12 && focus != null; i++)
                {
                    if (focus.Current.ProcessId != process)
                        return false;
                    var type = focus.Current.ControlType;
                    bool menu = type == ControlType.Menu || type == ControlType.MenuItem;
                    if (menu)
                        return false;
                    if (WordPasteGuard.DocumentNode(focus.Current.AutomationId, type == ControlType.Document, menu))
                        return true;
                    // Classic comment/reply panes expose an Edit on a native
                    // _WwG window, with neither Body ID nor Document ancestor.
                    // Require that exact Word-owned editor and a writable text
                    // selection; ribbon/search fields must still be rejected.
                    if (type == ControlType.Edit)
                    {
                        IntPtr handle = new IntPtr(focus.Current.NativeWindowHandle);
                        uint nativeProcess;
                        Native.GetWindowThreadProcessId(handle, out nativeProcess);
                        var name = new StringBuilder(64);
                        Native.GetClassName(handle, name, name.Capacity);
                        object provider;
                        if (handle != IntPtr.Zero && nativeProcess == (uint)process && name.ToString() == "_WwG" && focus.TryGetCurrentPattern(TextPattern.Pattern, out provider))
                        {
                            var selected = ((TextPattern)provider).GetSelection();
                            bool editable = selected.Length == 1 && object.Equals(selected[0].GetAttributeValue(TextPattern.IsReadOnlyAttribute), false);
                            if (WordPasteGuard.DocumentNode(focus.Current.AutomationId, false, false, name.ToString(), editable))
                                return true;
                        }
                    }

                    focus = TreeWalker.ControlViewWalker.GetParent(focus);
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool FocusedMenu(IntPtr owner)
        {
            try
            {
                if (!IsWord(owner) || Owner(Native.GetForegroundWindow()) != owner)
                    return false;
                var focus = EmbeddedWebViewFocus.Focused(owner);
                if (Owned(owner, focus))
                    for (int i = 0; i < 10 && focus != null; i++)
                    {
                        if (WordPasteGuard.Label(focus.Current.Name))
                            return true;
                        focus = TreeWalker.ControlViewWalker.GetParent(focus);
                    }

                // Office can leave UIA focus in Body while a separate native paste popup owns input.
                uint process;
                uint thread = Native.GetWindowThreadProcessId(owner, out process);
                bool found = false;
                int visited = 0;
                Native.EnumThreadWindows(thread, (popup, unused) =>
                {
                    if (++visited > 64 || found)
                        return false;
                    if (popup == owner || !Native.IsWindowVisible(popup) || Owner(popup) != owner)
                        return true;
                    try
                    {
                        var menu = AutomationElement.FromHandle(popup);
                        if (menu == null || menu.Current.ProcessId != (int)process)
                            return true;
                        var label = menu.FindFirst(TreeScope.Element | TreeScope.Descendants, new OrCondition(new PropertyCondition(AutomationElement.NameProperty, "Paste Options", PropertyConditionFlags.IgnoreCase), new PropertyCondition(AutomationElement.NameProperty, "Paste Options:", PropertyConditionFlags.IgnoreCase), new PropertyCondition(AutomationElement.NameProperty, "Paste Options (Ctrl)", PropertyConditionFlags.IgnoreCase), new PropertyCondition(AutomationElement.NameProperty, "Paste Options: (Ctrl)", PropertyConditionFlags.IgnoreCase)));
                        found = label != null && !label.Current.IsOffscreen;
                    }
                    catch (Exception)
                    {
                    }

                    return !found;
                }, IntPtr.Zero);
                return found;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
