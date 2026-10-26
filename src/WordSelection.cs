// KeyJaya: Late-bound Word selection edits with font/style preservation and grouped native undo.
// Only the foreground selection is used; open documents are not enumerated or rewritten as files.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace KeyJaya
{
    using System.Text;

    // Only the foreground Word selection is touched. No document enumeration or clipboard use.
    public static class WordSelection
    {
        public static Action<string> Trace;
        static void Detail(string value)
        {
            if (Trace != null)
                Trace("Word formatting " + value);
        }

        static object Invoke(object obj, string name, BindingFlags kind, object[] args)
        {
            var watch = Trace == null ? null : System.Diagnostics.Stopwatch.StartNew();
            try
            {
                return obj.GetType().InvokeMember(name, kind, null, obj, args);
            }
            finally
            {
                if (watch != null && watch.ElapsedMilliseconds >= 50)
                    Detail("slow call=" + name + " kind=" + kind + " ms=" + watch.ElapsedMilliseconds);
            }
        }

        static object Get(object obj, string name)
        {
            return Invoke(obj, name, BindingFlags.GetProperty, null);
        }

        static object Call(object obj, string name, params object[] args)
        {
            return Invoke(obj, name, BindingFlags.InvokeMethod, args);
        }

        static void Set(object obj, string name, object value)
        {
            Invoke(obj, name, BindingFlags.SetProperty, new[] { value });
        }

        static string Text(object range)
        {
            return ((string)Get(range, "Text") ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        }

        static void Free(object value)
        {
            try
            {
                if (value != null && Marshal.IsComObject(value))
                    Marshal.ReleaseComObject(value);
            }
            catch (Exception)
            {
            }
        }

        // Retain a runtime-managed COM reference, so identity is compared within the
        // checking apartment rather than comparing raw proxy pointers across threads.
        sealed class DocumentOwner : IDisposable
        {
            object document;
            public DocumentOwner(object value)
            {
                IntPtr pointer = Marshal.GetIUnknownForObject(value);
                try
                {
                    document = Marshal.GetObjectForIUnknown(pointer);
                }
                finally
                {
                    Marshal.Release(pointer);
                }
            }

            public bool Matches(object value)
            {
                lock (this)
                {
                    if (document == null)
                        return false;
                    IntPtr saved = Marshal.GetIUnknownForObject(document), current = IntPtr.Zero;
                    try
                    {
                        current = Marshal.GetIUnknownForObject(value);
                        return saved == current;
                    }
                    finally
                    {
                        if (current != IntPtr.Zero)
                            Marshal.Release(current);
                        Marshal.Release(saved);
                    }
                }
            }

            public void Dispose()
            {
                object releasing;
                lock (this)
                {
                    releasing = document;
                    document = null;
                }

                Free(releasing);
            }
        }

        public sealed class FontSnapshot : IDisposable
        {
            internal int ContinuationItalic = int.MinValue;
            internal readonly string Original;
            internal readonly int Start, Story;
            internal readonly List<object> Fonts = new List<object>();
            DocumentOwner documentIdentity;
            int users;
            bool disposed;
            internal FontSnapshot(object document, int start, int story, string original)
            {
                Original = original;
                Start = start;
                Story = story;
                documentIdentity = new DocumentOwner(document);
            }

            internal bool Matches(object document, int start, int story, string output)
            {
                lock (this)
                {
                    if (disposed || documentIdentity == null || Start != start || Story != story || Original != output)
                        return false;
                    return documentIdentity.Matches(document);
                }
            }

            internal bool TryContinuation(object document, int start, int story, out int italic)
            {
                lock (this)
                {
                    italic = ContinuationItalic;
                    return !disposed && documentIdentity != null && Start == start && Story == story && (italic == 0 || italic == -1) && documentIdentity.Matches(document);
                }
            }

            internal object[] Acquire()
            {
                lock (this)
                {
                    if (disposed)
                        throw new InvalidOperationException("Original font snapshot expired");
                    users++;
                    return Fonts.ToArray();
                }
            }

            internal void Release()
            {
                lock (this)
                {
                    users--;
                    if (users == 0 && disposed)
                        FreeFonts();
                }
            }

            void FreeFonts()
            {
                object[] releasing = Fonts.ToArray();
                Fonts.Clear();
                DocumentOwner releasingIdentity = documentIdentity;
                documentIdentity = null;
                foreach (object font in releasing)
                    Free(font);
                if (releasingIdentity != null)
                    releasingIdentity.Dispose();
            }

            public void Dispose()
            {
                lock (this)
                {
                    if (disposed)
                        return;
                    disposed = true;
                    if (users == 0)
                        FreeFonts();
                }
            }
        }

        static object Range(object story, int start, int end)
        {
            object range = Get(story, "Duplicate");
            try
            {
                Call(range, "SetRange", start, end);
                return range;
            }
            catch
            {
                Free(range);
                throw;
            }
        }

        public sealed class VisibleMap
        {
            public readonly string Value;
            public readonly int Start, End;
            readonly int[] positions;
            VisibleMap(string value, int start, int end, int[] offsets)
            {
                Value = value;
                Start = start;
                End = end;
                positions = offsets;
            }

            public static VisibleMap Create(string raw, int start, int end, int[] deletedStarts, int[] deletedEnds)
            {
                if (raw == null || start < 0 || end < start || end - start > 400000 || deletedStarts == null || deletedEnds == null || deletedStarts.Length != deletedEnds.Length)
                    return null;
                var hidden = new bool[end - start];
                for (int n = 0; n < deletedStarts.Length; n++)
                {
                    if (deletedEnds[n] < deletedStarts[n])
                        return null;
                    for (int at = Math.Max(start, deletedStarts[n]); at < Math.Min(end, deletedEnds[n]); at++)
                        hidden[at - start] = true;
                }

                var offsets = new List<int>();
                for (int n = 0; n < hidden.Length; n++)
                    if (!hidden[n])
                        offsets.Add(start + n);
                string value;
                if (raw.Length == offsets.Count)
                    value = raw;
                else if (raw.Length == hidden.Length)
                {
                    var text = new StringBuilder();
                    for (int n = 0; n < raw.Length; n++)
                        if (!hidden[n])
                            text.Append(raw[n]);
                    value = text.ToString();
                }
                else
                    return null;
                return new VisibleMap(value, start, end, offsets.ToArray());
            }

            public bool MatchesRaw(string raw)
            {
                if (raw == Value)
                    return true;
                if (raw == null || raw.Length != End - Start)
                    return false;
                for (int n = 0; n < positions.Length; n++)
                    if (raw[positions[n] - Start] != Value[n])
                        return false;
                return true;
            }

            public VisibleMap Edited(int offset, int length, VisibleMap inserted, int nativeGrowth)
            {
                if (offset < 0 || length < 0 || offset > Value.Length - length || inserted == null)
                    return null;
                int newEnd = checked(End + nativeGrowth);
                var updated = new List<int>();
                for (int n = 0; n < offset; n++)
                    updated.Add(positions[n]);
                updated.AddRange(inserted.positions);
                for (int n = offset + length; n < positions.Length; n++)
                    updated.Add(checked(positions[n] + nativeGrowth));
                for (int n = 0; n < updated.Count; n++)
                    if (updated[n] < Start || updated[n] >= newEnd || n > 0 && updated[n] <= updated[n - 1])
                        return null;
                return new VisibleMap(Value.Remove(offset, length).Insert(offset, inserted.Value), Start, newEnd, updated.ToArray());
            }

            public int NativeOffset(int offset)
            {
                if (offset < 0 || offset > positions.Length)
                    throw new ArgumentOutOfRangeException();
                return offset == positions.Length ? End : positions[offset];
            }

            public int[][] Spans(int offset, int length)
            {
                if (offset < 0 || length < 0 || offset > positions.Length - length)
                    throw new ArgumentOutOfRangeException();
                var result = new List<int[]>();
                int at = offset, stop = offset + length;
                while (at < stop)
                {
                    int begin = positions[at++], end = begin + 1;
                    while (at < stop && positions[at] == end)
                    {
                        at++;
                        end++;
                    }

                    result.Add(new[] { begin, end });
                }

                return result.ToArray();
            }
        }

        static object[] RevisionItems(object revisions, int count)
        {
            if (count == 0)
                return new object[0];
            var result = new List<object>();
            object enumerator = null;
            try
            {
                enumerator = Get(revisions, "_NewEnum");
                var managed = enumerator as System.Collections.IEnumerator;
                if (managed != null)
                {
                    while (managed.MoveNext())
                    {
                        result.Add(managed.Current);
                        if (result.Count > 10000)
                            throw new InvalidOperationException("Revision enumeration limit exceeded");
                    }
                }
                else
                {
                    var iterator = (System.Runtime.InteropServices.ComTypes.IEnumVARIANT)enumerator;
                    object[] slot = new object[1];
                    while (iterator.Next(1, slot, IntPtr.Zero) == 0)
                    {
                        result.Add(slot[0]);
                        slot[0] = null;
                        if (result.Count > 10000)
                            throw new InvalidOperationException("Revision enumeration limit exceeded");
                    }
                }

                return result.ToArray();
            }
            catch
            {
                foreach (object revision in result)
                    Free(revision);
                throw;
            }
            finally
            {
                Free(enumerator);
            }
        }

        static VisibleMap Visible(object range)
        {
            object revisions = null;
            object[] revisionItems = null;
            try
            {
                int start = Convert.ToInt32(Get(range, "Start")), end = Convert.ToInt32(Get(range, "End"));
                revisions = Get(range, "Revisions");
                int count = Convert.ToInt32(Get(revisions, "Count"));
                if (count > 10000)
                    return null;
                var a = new List<int>();
                var b = new List<int>();
                revisionItems = RevisionItems(revisions, count);
                for (int n = 1; n <= revisionItems.Length; n++)
                {
                    object revision = null, part = null;
                    try
                    {
                        revision = revisionItems[n - 1];
                        int type = Convert.ToInt32(Get(revision, "Type"));
                        if (type == 7 || type == 9 || type >= 16)
                            return null;
                        if (type != 2 && type != 14)
                            continue;
                        part = Get(revision, "Range");
                        a.Add(Convert.ToInt32(Get(part, "Start")));
                        b.Add(Convert.ToInt32(Get(part, "End")));
                    }
                    finally
                    {
                        Free(part);
                    }
                }

                return VisibleMap.Create(Text(range), start, end, a.ToArray(), b.ToArray());
            }
            finally
            {
                if (revisionItems != null)
                    foreach (object revision in revisionItems)
                        Free(revision);
                Free(revisions);
            }
        }

        public static string SelectedFontName(IntPtr owner, string expected)
        {
            object app = null, window = null, selection = null, scope = null, font = null;
            try
            {
                if (!WordPasteOptions.IsWord(owner) || owner != Native.GetForegroundWindow())
                    return null;
                app = WordApplication.ForWindow(owner);
                if (app == null)
                    return null;
                window = Get(app, "ActiveWindow");
                if (WordPasteOptions.Owner(new IntPtr(Convert.ToInt64(Get(window, "Hwnd")))) != WordPasteOptions.Owner(owner))
                    return null;
                selection = Get(app, "Selection");
                scope = Get(selection, "Range");
                if (!SupportedStory(Convert.ToInt32(Get(scope, "StoryType"))))
                    return null;
                var map = Visible(scope);
                if (map == null || SelectionFormatting.WordOwnedLength(expected, map.Value, map.Value.Length) < 0)
                    return null;
                font = Get(scope, "Font");
                string name = Convert.ToString(Get(font, "Name"));
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                Free(font);
                Free(scope);
                Free(selection);
                Free(window);
                Free(app);
            }
        }

        static void VisibleFont(object scope, VisibleMap map, int offset, int length, object font, Func<bool> context = null)
        {
            foreach (var span in map.Spans(offset, length))
            {
                object part = null;
                try
                {
                    if (context != null && !context())
                        throw new InvalidOperationException("Formatting context changed");
                    part = Range(scope, span[0], span[1]);
                    Set(part, "Font", font);
                }
                finally
                {
                    Free(part);
                }
            }
        }

        static FontSnapshot CaptureFonts(object document, object story, int start, string input, VisibleMap verifiedMap)
        {
            if (input.Length > 160 || input.IndexOfAny(new[] { ' ', '\r', '\n', '\t' }) >= 0)
                return null;
            int storyType = Convert.ToInt32(Get(story, "StoryType"));
            var snapshot = new FontSnapshot(document, start, storyType, input);
            try
            {
                var map = verifiedMap;
                if (map == null || map.Start != start || map.Value != input)
                    throw new InvalidOperationException("Font snapshot text changed");
                for (int n = 0; n < input.Length; n++)
                {
                    object part = null, font = null;
                    try
                    {
                        int position = map.NativeOffset(n);
                        part = storyType == 1 ? Call(document, "Range", position, position + 1) : Range(story, position, position + 1);
                        font = Get(part, "Font");
                        snapshot.Fonts.Add(Get(font, "Duplicate"));
                        if (n == input.Length - 1)
                            snapshot.ContinuationItalic = Convert.ToInt32(Get(font, "Italic"));
                    }
                    finally
                    {
                        Free(font);
                        Free(part);
                    }
                }

                return snapshot;
            }
            catch
            {
                snapshot.Dispose();
                throw;
            }
        }

        static void RestoreFonts(FontSnapshot snapshot, object result, int start, Func<bool> context)
        {
            object[] fonts = snapshot.Acquire();
            try
            {
                var map = Visible(result);
                if (map == null || map.Value != snapshot.Original)
                    throw new InvalidOperationException("Font restoration text changed");
                for (int n = 0; n < fonts.Length; n++)
                {
                    object part = null;
                    try
                    {
                        if (!context())
                            throw new InvalidOperationException("Font restoration context changed");
                        int position = map.NativeOffset(n);
                        part = Range(result, position, position + 1);
                        Set(part, "Font", fonts[n]);
                    }
                    finally
                    {
                        Free(part);
                    }
                }
            }
            finally
            {
                snapshot.Release();
            }
        }

        public static bool SupportedStory(int story)
        {
            return story == 1 || story == 2 || story == 3 || story >= 6 && story <= 11;
        }

        // Word's UIA character endpoints can disagree at the first character after a
        // paragraph break. Own bounded typed text by Word's native story positions instead.
        // Native revision coordinates remain stable when UI Automation exposes both
        // the deleted original and inserted replacement in Simple Markup.
        public sealed class ReplayAnchor
        {
            readonly IntPtr owner;
            readonly int start, story, continuationItalic;
            readonly string original;
            readonly DocumentOwner identity;
            readonly VisibleMap originalMap;
            readonly object recordGate = new object ();
            VisibleMap committedMap;
            object deferredUndo;
            ReplayAnchor(IntPtr window, int begin, int kind, string text, object document, int italic, VisibleMap map)
            {
                owner = window;
                start = begin;
                story = kind;
                original = text;
                identity = new DocumentOwner(document);
                continuationItalic = italic;
                originalMap = map;
            }

            ~ReplayAnchor()
            {
                try
                {
                    identity.Dispose();
                    Free(deferredUndo);
                }
                catch
                {
                }
            }

            public static ReplayAnchor Capture(IntPtr owner, string expected, FontSnapshot continuation = null)
            {
                object app = null, selection = null, scope = null, document = null, font = null, tail = null;
                try
                {
                    if (!WordPasteOptions.IsWord(owner) || owner != Native.GetForegroundWindow())
                        return null;
                    app = WordApplication.ForWindow(owner);
                    if (app == null || !OwnedTextAnchor.Foreground(app, owner))
                        return null;
                    selection = Get(app, "Selection");
                    scope = Get(selection, "Range");
                    var map = Visible(scope);
                    int length = map == null ? -1 : SelectionFormatting.WordOwnedLength(expected, map.Value, map.Value.Length);
                    if (length < 1)
                        return null;
                    int kind = Convert.ToInt32(Get(scope, "StoryType"));
                    if (!SupportedStory(kind))
                        return null;
                    document = Get(selection, "Document");
                    int begin = Convert.ToInt32(Get(selection, "Start")), italic = int.MinValue;
                    bool inherited = continuation != null && continuation.TryContinuation(document, begin, kind, out italic);
                    if (!inherited)
                    {
                        tail = Range(scope, map.NativeOffset(length - 1), map.NativeOffset(length));
                        font = Get(tail, "Font");
                        italic = Convert.ToInt32(Get(font, "Italic"));
                    }

                    Detail("continuation captured italic=" + italic + " length=" + length);
                    return new ReplayAnchor(owner, begin, kind, expected, document, italic, map);
                }
                catch
                {
                    return null;
                }
                finally
                {
                    Free(font);
                    Free(tail);
                    Free(document);
                    Free(scope);
                    Free(selection);
                    Free(app);
                }
            }

            // Keep caret-format restoration in the same native undo step as conversion.
            // Ownership passes here only after the replacement has been fully verified.
            internal bool DeferRecord(object document, int begin, int kind, VisibleMap map, object undo)
            {
                lock (recordGate)
                {
                    if (deferredUndo != null || begin != start || kind != story || map == null || map.Start != start || !identity.Matches(document))
                        return false;
                    committedMap = map;
                    deferredUndo = undo;
                    return true;
                }
            }

            public bool FinishRecord()
            {
                lock (recordGate)
                {
                    if (deferredUndo == null)
                        return true;
                    try
                    {
                        if (Convert.ToInt32(Get(deferredUndo, "CustomRecordLevel")) > 0)
                            Call(deferredUndo, "EndCustomRecord");
                        object finished = deferredUndo;
                        deferredUndo = null;
                        Free(finished);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Detail("continuation record finish=" + ex.GetType().Name);
                        return false;
                    }
                }
            }

            bool RestoreContinuation(object selection)
            {
                lock (recordGate)
                {
                    if (deferredUndo == null || continuationItalic != 0 && continuationItalic != -1)
                        return true;
                    object font = null;
                    try
                    {
                        font = Get(selection, "Font");
                        Detail("continuation restore wanted=" + continuationItalic + " actual=" + Convert.ToInt32(Get(font, "Italic")));
                        if (Convert.ToInt32(Get(font, "Italic")) != continuationItalic)
                            Set(font, "Italic", continuationItalic);
                        return Convert.ToInt32(Get(font, "Italic")) == continuationItalic;
                    }
                    finally
                    {
                        Free(font);
                    }
                }
            }

            public bool Resume(string replacement, bool selected, bool typing)
            {
                object app = null, selection = null, scope = null, document = null, part = null;
                try
                {
                    if (owner != Native.GetForegroundWindow())
                        return false;
                    app = WordApplication.ForWindow(owner);
                    if (app == null || !OwnedTextAnchor.Foreground(app, owner))
                        return false;
                    selection = Get(app, "Selection");
                    document = Get(selection, "Document");
                    if (!identity.Matches(document))
                        return false;
                    scope = Get(selection, "Range");
                    if (Convert.ToInt32(Get(scope, "StoryType")) != story)
                        return false;
                    int a = Convert.ToInt32(Get(selection, "Start")), b = Convert.ToInt32(Get(selection, "End"));
                    string expected = replacement ?? original;
                    var map = replacement == null ? originalMap : committedMap;
                    // Reuse the transaction's verified revision projection. Enumerating Word's
                    // Revisions here can reset pending typing format even without changing text.
                    if (map != null && map.Value == expected)
                    {
                        part = Range(scope, start, map.End);
                        if (!map.MatchesRaw(Text(part)))
                            return false;
                    }
                    else
                    {
                        if (a < start)
                            return false;
                        part = Get(scope, "Duplicate");
                        Call(part, "WholeStory");
                        int end = Convert.ToInt32(Get(part, "End"));
                        Call(part, "SetRange", start, (int)Math.Min(end, (long)Math.Max(a, b) + expected.Length * 4L + 1024));
                        map = Visible(part);
                        if (map == null || !map.Value.StartsWith(expected, StringComparison.Ordinal))
                            return false;
                    }

                    int targetEnd = map.NativeOffset(expected.Length);
                    if (a != b)
                    {
                        if (a != start || b != targetEnd)
                            return false;
                        if (!typing)
                            return FinishRecord();
                        Call(selection, "Collapse", 0);
                        if (Convert.ToInt32(Get(selection, "Start")) != targetEnd || Convert.ToInt32(Get(selection, "End")) != targetEnd)
                            return false;
                        return RestoreContinuation(selection) && FinishRecord();
                    }

                    if (a < start || a > targetEnd)
                        return false;
                    return (a != targetEnd || RestoreContinuation(selection)) && FinishRecord();
                }
                catch
                {
                    return false;
                }
                finally
                {
                    Free(part);
                    Free(document);
                    Free(scope);
                    Free(selection);
                    Free(app);
                }
            }
        }

        public sealed class OwnedTextAnchor
        {
            readonly IntPtr owner;
            readonly string expected;
            readonly int start, story;
            readonly DocumentOwner identity;
            OwnedTextAnchor(IntPtr window, string text, int begin, int storyType, object document)
            {
                owner = window;
                expected = text;
                start = begin;
                story = storyType;
                identity = new DocumentOwner(document);
            }

            ~OwnedTextAnchor()
            {
                try
                {
                    identity.Dispose();
                }
                catch (Exception)
                {
                }
            }

            public static bool Foreground(object app, IntPtr owner)
            {
                object window = null;
                try
                {
                    if (owner != Native.GetForegroundWindow())
                        return false;
                    window = Get(app, "ActiveWindow");
                    IntPtr hwnd = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                    uint a, b;
                    Native.GetWindowThreadProcessId(owner, out a);
                    Native.GetWindowThreadProcessId(hwnd, out b);
                    return a != 0 && a == b && WordPasteOptions.Owner(hwnd) == WordPasteOptions.Owner(owner);
                }
                finally
                {
                    Free(window);
                }
            }

            public static OwnedTextAnchor Capture(IntPtr owner, string expected, bool singleLetter = true)
            {
                if (!WordPasteOptions.IsWord(owner) || string.IsNullOrEmpty(expected) || expected.Length > 160 || expected.IndexOfAny(new[] { '\0', '\a' }) >= 0 || singleLetter && !SingleLetters.Supported(expected))
                    return null;
                object app = null, selection = null, document = null, scope = null, letter = null;
                try
                {
                    app = WordApplication.ForWindow(owner);
                    if (app == null || !Foreground(app, owner))
                        return null;
                    selection = Get(app, "Selection");
                    int begin = Convert.ToInt32(Get(selection, "Start")), end = Convert.ToInt32(Get(selection, "End"));
                    if (begin != end || begin < expected.Length)
                        return null;
                    scope = Get(selection, "Range");
                    int story = Convert.ToInt32(Get(scope, "StoryType"));
                    if (!SupportedStory(story))
                        return null;
                    letter = Range(scope, begin - expected.Length, begin);
                    if (Text(letter) != expected)
                        return null;
                    document = Get(selection, "Document");
                    return new OwnedTextAnchor(owner, expected, begin - expected.Length, story, document);
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    Free(letter);
                    Free(scope);
                    Free(document);
                    Free(selection);
                    Free(app);
                }
            }

            public static bool Positions(int start, int end, int ownedStart, bool caret, int length = 1)
            {
                return ownedStart >= 0 && length > 0 && length <= 160 && ownedStart <= int.MaxValue - length && (caret ? start == ownedStart + length && end == start : start == ownedStart && end == ownedStart + length);
            }

            bool Check(bool caret, bool select = false, bool restore = false)
            {
                object app = null, selection = null, document = null, scope = null, letter = null;
                try
                {
                    app = WordApplication.ForWindow(owner);
                    if (app == null || !Foreground(app, owner))
                        return false;
                    selection = Get(app, "Selection");
                    document = Get(selection, "Document");
                    if (!identity.Matches(document))
                        return false;
                    scope = Get(selection, "Range");
                    if (Convert.ToInt32(Get(scope, "StoryType")) != story)
                        return false;
                    int a = Convert.ToInt32(Get(selection, "Start")), b = Convert.ToInt32(Get(selection, "End"));
                    bool matches = Positions(a, b, start, caret, expected.Length);
                    if (restore)
                        matches = Positions(a, b, start, true, expected.Length) || Positions(a, b, start, false, expected.Length) || a == start && b == start;
                    if (!matches)
                        return false;
                    letter = Range(scope, start, start + expected.Length);
                    if (Text(letter) != expected)
                        return false;
                    if (!caret && !restore && Text(selection) != expected)
                        return false;
                    if (select)
                    {
                        Call(letter, "Select");
                        return Positions(Convert.ToInt32(Get(selection, "Start")), Convert.ToInt32(Get(selection, "End")), start, false, expected.Length) && Text(selection) == expected;
                    }

                    if (restore)
                    {
                        Call(letter, "Collapse", 0);
                        Call(letter, "Select");
                        return Positions(Convert.ToInt32(Get(selection, "Start")), Convert.ToInt32(Get(selection, "End")), start, true, expected.Length);
                    }

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    Free(letter);
                    Free(scope);
                    Free(document);
                    Free(selection);
                    Free(app);
                }
            }

            public bool AtCaret()
            {
                return Check(true);
            }

            public bool Selected()
            {
                return Check(false);
            }

            public bool Select()
            {
                return Check(true, true);
            }

            public bool Restore()
            {
                return Check(false, false, true);
            }
        }

        static VisibleMap ReplacePatch(object story, VisibleMap map, TextPatch patch, ref bool changed)
        {
            object part = null, style = null, sourceFont = null, font = null;
            try
            {
                if (map == null || patch.Offset < 0 || patch.Offset > map.Value.Length - patch.Length || map.Value.Substring(patch.Offset, patch.Length) != patch.Before)
                    throw new InvalidOperationException("Selected text changed");
                int[][] bounds = map.Spans(patch.Offset, patch.Length);
                if (bounds.Length > 1)
                    throw new InvalidOperationException("Patch crosses an existing deletion");
                int begin = patch.Length == 0 ? map.NativeOffset(patch.Offset) : bounds[0][0], end = patch.Length == 0 ? begin : bounds[0][1];
                part = Range(story, begin, end);
                if (patch.After.Length > 0 && (patch.Length != 1 || patch.After.Length != 1))
                {
                    int anchor = Math.Min(patch.Offset, Math.Max(0, map.Value.Length - 1));
                    int native = map.NativeOffset(anchor);
                    style = Range(story, native, native + 1);
                    sourceFont = Get(style, "Font");
                    font = Get(sourceFont, "Duplicate");
                }

                Set(part, "Text", patch.After.Replace('\n', '\r'));
                changed = true;
                if (patch.After.Length > 0)
                {
                    var inserted = Visible(part);
                    if (inserted == null || inserted.Value != patch.After)
                        throw new InvalidOperationException("Inserted revision text unavailable");
                    if (font != null)
                        VisibleFont(part, inserted, 0, inserted.Value.Length, font);
                    return inserted;
                }

                return VisibleMap.Create("", begin, begin, new int[0], new int[0]);
            }
            finally
            {
                Free(font);
                Free(sourceFont);
                Free(style);
                Free(part);
            }
        }

        static bool DisplayStyle(object range)
        {
            object style = null;
            try
            {
                style = Get(range, "Style");
                string name = style as string;
                if (name == null)
                    name = Convert.ToString(Get(style, "NameLocal"));
                if (System.Text.RegularExpressions.Regex.IsMatch(name, @"^(?:Heading [1-9]|Title|Subtitle)(?:$|\s)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
                if (!(style is string))
                {
                    name = Convert.ToString(Get(style, "BaseStyle"));
                    return System.Text.RegularExpressions.Regex.IsMatch(name, @"^(?:Heading [1-9]|Title|Subtitle)(?:$|\s)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Free(style);
            }
        }

        public static bool Replace(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics = null, bool retain = true, string balaramWordFont = null, FontSnapshot restoreFonts = null, Action<FontSnapshot> captureFonts = null, ItalicSpan[] fontSpans = null, ReplayAnchor replay = null)
        {
            return ReplaceCore(owner, input, output, context, out handled, italics, retain, balaramWordFont, restoreFonts, captureFonts, fontSpans, false, replay);
        }

        public static bool ReplaceExpansion(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics = null, bool retain = false, ReplayAnchor replay = null)
        {
            return ReplaceCore(owner, input, output, context, out handled, italics, retain, null, null, null, null, true, replay);
        }

        public static ItalicSpan[] MergeVerseSpans(string output, ItalicSpan[] input)
        {
            if (input == null)
                return null;
            var merged = new List<ItalicSpan>();
            foreach (var span in input)
            {
                if (merged.Count > 0)
                {
                    var previous = merged[merged.Count - 1];
                    int begin = previous.Start + previous.Length;
                    bool gap = span.Start >= begin;
                    for (int n = begin; gap && n < span.Start; n++)
                        gap = n < output.Length && (char.IsWhiteSpace(output[n]) || char.IsPunctuation(output[n]));
                    if (previous.Force && span.Force && gap)
                    {
                        merged[merged.Count - 1] = new ItalicSpan(previous.Start, span.Start + span.Length - previous.Start, true);
                        continue;
                    }
                }

                merged.Add(span);
            }

            return merged.ToArray();
        }

        static bool ReplaceCore(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics, bool retain, string balaramWordFont, FontSnapshot restoreFonts, Action<FontSnapshot> captureFonts, ItalicSpan[] fontSpans, bool expansion, ReplayAnchor replay)
        {
            handled = WordPasteOptions.IsWord(owner);
            if (!handled)
                return false;
            object app = null, window = null, selection = null, selectionRange = null, document = null, undo = null, whole = null, nativeStory = null;
            bool begun = false, changed = false, ended = false;
            int lastStoryEnd = -1;
            string currentText = input;
            VisibleMap currentMap = null;
            int typingItalic = 0;
            var objects = new List<object>();
            FontSnapshot captured = null;
            try
            {
                Detail("entered");
                if (!context())
                    return false;
                TextPatch[] patches = expansion && SelectionConversion.ValidText(input) && SelectionConversion.ValidText(output) ? new[]
                {
                    new TextPatch(0, input, output)
                }

                : SelectionFormatting.Plan(input, output);
                italics = MergeVerseSpans(output, italics);
                if (patches == null)
                    return false;
                app = WordApplication.ForWindow(owner);
                if (app == null)
                    return false;
                window = Get(app, "ActiveWindow");
                uint foregroundPid, wordPid;
                Native.GetWindowThreadProcessId(owner, out foregroundPid);
                Native.GetWindowThreadProcessId(new IntPtr(Convert.ToInt64(Get(window, "Hwnd"))), out wordPid);
                if (wordPid != foregroundPid || foregroundPid == 0 || WordPasteOptions.Owner(new IntPtr(Convert.ToInt64(Get(window, "Hwnd")))) != WordPasteOptions.Owner(owner))
                    return false;
                Detail("application obtained");
                selection = Get(app, "Selection");
                selectionRange = Get(selection, "Range");
                int storyType = Convert.ToInt32(Get(selectionRange, "StoryType"));
                if (!SupportedStory(storyType))
                    return false;
                document = Get(selection, "Document");
                int start = Convert.ToInt32(Get(selection, "Start")), end = Convert.ToInt32(Get(selection, "End"));
                var selectedMap = Visible(selectionRange);
                int ownedLength = selectedMap == null ? -1 : SelectionFormatting.WordOwnedLength(input, selectedMap.Value, selectedMap.Value.Length);
                if (start == end || ownedLength < 0 || !context())
                {
                    Detail("range mismatch length=" + (end - start) + " expected=" + input.Length);
                    return false;
                }

                if (ownedLength != end - start)
                    Detail("excluded terminal paragraph marker");
                end = selectedMap.NativeOffset(ownedLength);
                whole = Range(selectionRange, start, end);
                if (Convert.ToInt32(Get(whole, "StoryType")) != storyType)
                    return false;
                if (restoreFonts != null && !restoreFonts.Matches(document, start, storyType, output))
                    return false;
                var initialMap = ownedLength == selectedMap.Value.Length && end == selectedMap.End ? selectedMap : Visible(whole);
                if (initialMap == null || initialMap.Value != input)
                    throw new InvalidOperationException("Original selection projection unavailable");
                if (captureFonts != null)
                    captured = CaptureFonts(document, whole, start, input, initialMap);
                object typingFont = Get(selection, "Font");
                objects.Add(typingFont);
                typingItalic = Convert.ToInt32(Get(typingFont, "Italic"));
                undo = Get(app, "UndoRecord");
                if (Convert.ToInt32(Get(undo, "CustomRecordLevel")) != 0)
                    return false;
                Call(undo, "StartCustomRecord", "KeyJaya selection conversion");
                begun = true;
                nativeStory = Get(whole, "Duplicate");
                objects.Add(nativeStory);
                Call(nativeStory, "WholeStory");
                lastStoryEnd = Convert.ToInt32(Get(nativeStory, "End"));
                currentMap = initialMap;
                foreach (TextPatch patch in patches)
                {
                    var beforePatch = currentMap;
                    if (beforePatch == null || beforePatch.Value != currentText || !beforePatch.MatchesRaw(Text(whole)) || Convert.ToInt32(Get(whole, "Start")) != start)
                    {
                        Detail("patch range check observed=" + (beforePatch == null ? -1 : beforePatch.Value.Length) + " expected=" + currentText.Length + " start=" + (Convert.ToInt32(Get(whole, "Start")) == start));
                        throw new InvalidOperationException("Selection moved or changed");
                    }

                    if (!context())
                        throw new InvalidOperationException("Conversion context changed");
                    int storyEndBefore = Convert.ToInt32(Get(nativeStory, "End"));
                    var insertedMap = ReplacePatch(whole, beforePatch, patch, ref changed);
                    currentText = currentText.Remove(patch.Offset, patch.Length).Insert(patch.Offset, patch.After);
                    // Replacing a final character can exclude the insertion from a live Range.
                    // Story growth includes retained tracked deletions as well as visible text.
                    int storyEndAfter = Convert.ToInt32(Get(nativeStory, "End"));
                    int nativeGrowth = storyEndAfter - storyEndBefore;
                    currentMap = beforePatch.Edited(patch.Offset, patch.Length, insertedMap, nativeGrowth);
                    if (currentMap == null || currentMap.Value != currentText)
                        throw new InvalidOperationException("Edited revision projection unavailable");
                    end = checked(end + nativeGrowth);
                    if (end < start || end > storyEndAfter)
                        throw new InvalidOperationException("Native replacement bounds unavailable");
                    Call(whole, "SetRange", start, end);
                    lastStoryEnd = storyEndAfter;
                }

                object result = Get(whole, "Duplicate");
                objects.Add(result);
                var resultMap = currentMap;
                if (resultMap == null || resultMap.Value != output || !resultMap.MatchesRaw(Text(result)) || !context())
                    throw new InvalidOperationException("Conversion result unavailable");
                if (italics != null)
                    foreach (var span in italics)
                    {
                        if (span.Start < 0 || span.Length < 1 || span.Start + span.Length > output.Length || !context())
                            throw new InvalidOperationException("Italic selection context changed");
                        object portion = null, font = null;
                        try
                        {
                            foreach (var segment in resultMap.Spans(span.Start, span.Length))
                            {
                                portion = Range(result, segment[0], segment[1]);
                                font = Get(portion, "Font");
                                if (span.Force || Convert.ToInt32(Get(font, "Bold")) != -1 && !DisplayStyle(portion))
                                {
                                    Set(font, "Italic", -1);
                                    changed = true;
                                }

                                Free(font);
                                font = null;
                                Free(portion);
                                portion = null;
                            }
                        }
                        finally
                        {
                            Free(font);
                            Free(portion);
                        }
                    }

                if (balaramWordFont != null)
                {
                    if (!context() || input.Length > 160 || input.IndexOfAny(new[] { ' ', '\r', '\n', '\t' }) >= 0)
                        throw new InvalidOperationException("Mixed word font context changed");
                    if (fontSpans == null)
                    {
                        foreach (var segment in resultMap.Spans(0, output.Length))
                        {
                            object visiblePart = null, font = null;
                            try
                            {
                                visiblePart = Range(result, segment[0], segment[1]);
                                font = Get(visiblePart, "Font");
                                Set(font, "Name", balaramWordFont);
                                changed = true;
                            }
                            finally
                            {
                                Free(font);
                                Free(visiblePart);
                            }
                        }
                    }
                    else
                        foreach (var span in fontSpans)
                        {
                            if (span.Start < 0 || span.Length < 1 || span.Start + span.Length > output.Length || !context())
                                throw new InvalidOperationException("Compound font scope changed");
                            object part = null, font = null;
                            try
                            {
                                foreach (var segment in resultMap.Spans(span.Start, span.Length))
                                {
                                    part = Range(result, segment[0], segment[1]);
                                    font = Get(part, "Font");
                                    Set(font, "Name", balaramWordFont);
                                    changed = true;
                                    Free(font);
                                    font = null;
                                    Free(part);
                                    part = null;
                                }
                            }
                            finally
                            {
                                Free(font);
                                Free(part);
                            }
                        }
                }

                if (restoreFonts != null)
                {
                    if (!context())
                        throw new InvalidOperationException("Font restoration context changed");
                    RestoreFonts(restoreFonts, result, start, context);
                    changed = true;
                }

                if (!retain)
                    Call(result, "Collapse", 0);
                Call(result, "Select");
                if (!retain && restoreFonts == null && italics != null && (typingItalic == 0 || typingItalic == -1))
                {
                    object font = Get(selection, "Font");
                    try
                    {
                        Set(font, "Italic", typingItalic);
                    }
                    finally
                    {
                        Free(font);
                    }
                }

                if (captured != null)
                {
                    captureFonts(captured);
                    captured = null;
                }

                if (replay != null && replay.DeferRecord(document, start, storyType, resultMap, undo))
                {
                    ended = true;
                    undo = null;
                }
                else
                {
                    Call(undo, "EndCustomRecord");
                    ended = true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Detail("exception=" + ex.GetType().Name + " code=" + ex.HResult);
                if (ex is InvalidOperationException && ex.TargetSite != null && ex.TargetSite.DeclaringType == typeof(WordSelection))
                    Detail("reason=" + ex.Message);
                if (ex.InnerException != null)
                    Detail("inner=" + ex.InnerException.GetType().Name + " code=" + ex.InnerException.HResult);
                if (begun && !ended)
                {
                    try
                    {
                        Call(undo, "EndCustomRecord");
                        ended = true;
                    }
                    catch (Exception)
                    {
                    }
                }

                if (changed)
                    try
                    {
                        bool rollback = context();
                        if (!rollback && whole != null && nativeStory != null && Native.GetForegroundWindow() != owner)
                        {
                            rollback = currentMap != null && currentMap.Value == currentText && currentMap.MatchesRaw(Text(whole)) && Convert.ToInt32(Get(nativeStory, "End")) == lastStoryEnd;
                        }

                        if (rollback)
                        {
                            Call(document, "Undo", 1);
                            Detail("unfinished transaction rolled back");
                        }
                        else
                            Detail("rollback skipped: ownership changed");
                    }
                    catch (Exception)
                    {
                        Detail("rollback unavailable");
                    }

                return false;
            }
            finally
            {
                if (captured != null)
                    captured.Dispose();
                if (begun && !ended)
                    try
                    {
                        Call(undo, "EndCustomRecord");
                    }
                    catch (Exception)
                    {
                    }

                for (int i = objects.Count - 1; i >= 0; i--)
                    Free(objects[i]);
                Free(whole);
                Free(undo);
                Free(document);
                Free(selectionRange);
                Free(selection);
                Free(window);
                Free(app);
            }
        }
    }
}
