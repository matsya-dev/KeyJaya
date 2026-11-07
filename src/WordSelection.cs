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
            var watch = Trace == null && !WordNativeTiming.Active ? null : System.Diagnostics.Stopwatch.StartNew();
            try
            {
                return obj.GetType().InvokeMember(name, kind, null, obj, args);
            }
            finally
            {
                if (watch != null)
                    WordNativeTiming.Record(name, kind, watch.ElapsedTicks);
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
                {
                    if (WordNativeTiming.Active)
                        WordNativeTiming.Release(() => Marshal.ReleaseComObject(value));
                    else
                        Marshal.ReleaseComObject(value);
                }
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
            readonly Action<object> releaseFont;
            int users;
            bool disposed;
            internal FontSnapshot(object document, int start, int story, string original, Action<object> release = null)
            {
                Original = original;
                Start = start;
                Story = story;
                documentIdentity = new DocumentOwner(document);
                releaseFont = release ?? Free;
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

            void FreeFonts(bool background = false)
            {
                object[] releasing = Fonts.ToArray();
                Fonts.Clear();
                DocumentOwner releasingIdentity = documentIdentity;
                documentIdentity = null;
                Action cleanup = () =>
                {
                    foreach (object font in releasing)
                        releaseFont(font);
                    if (releasingIdentity != null)
                        releasingIdentity.Dispose();
                };
                if (background)
                    System.Threading.ThreadPool.QueueUserWorkItem(unused => cleanup());
                else
                    cleanup();
            }

            // Navigation runs inside the low-level hook. Invalidate its undo
            // ticket immediately, but never wait there for Word to release COM
            // font/document references: Word can itself be waiting on that hook.
            internal void Retire()
            {
                lock (this)
                {
                    if (disposed)
                        return;
                    disposed = true;
                    if (users == 0)
                        FreeFonts(true);
                }
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

        static void RevisionBatches(System.Runtime.InteropServices.ComTypes.IEnumVARIANT iterator, int count, List<object> result)
        {
            int size = Math.Min(32, count + 1);
            var batch = new object[size];
            IntPtr fetched = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                while (true)
                {
                    Marshal.WriteInt32(fetched, -1);
                    int status = WordNativeTiming.Active ? WordNativeTiming.Measure("Revision.Next", () => iterator.Next(size, batch, fetched)) : iterator.Next(size, batch, fetched);
                    if (status < 0)
                        Marshal.ThrowExceptionForHR(status);
                    int actual = Marshal.ReadInt32(fetched);
                    if (status != 0 && status != 1 || actual < 0 || actual > size || status == 0 && actual != size || status == 1 && actual == size)
                        throw new InvalidOperationException("Revision batch unavailable");
                    for (int n = actual; n < size; n++)
                        if (batch[n] != null)
                            throw new InvalidOperationException("Unexpected revision batch item");
                    for (int n = 0; n < actual; n++)
                    {
                        if (batch[n] == null)
                            throw new InvalidOperationException("Missing revision batch item");
                        result.Add(batch[n]);
                        batch[n] = null;
                        if (result.Count > 10000)
                            throw new InvalidOperationException("Revision enumeration limit exceeded");
                    }

                    if (status == 1)
                        return;
                }
            }
            finally
            {
                foreach (object item in batch)
                    Free(item);
                Marshal.FreeHGlobal(fetched);
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
                var iterator = enumerator as System.Runtime.InteropServices.ComTypes.IEnumVARIANT;
                if (iterator == null)
                {
                    // The Framework bridges COM enumerators to IEnumerator.
                    // Its public adapter lends the same underlying iterator;
                    // keep its owner alive and never release that alias twice.
                    var adapter = enumerator as ICustomAdapter;
                    if (adapter != null)
                        iterator = adapter.GetUnderlyingObject() as System.Runtime.InteropServices.ComTypes.IEnumVARIANT;
                }

                var managed = enumerator as System.Collections.IEnumerator;
                if (iterator != null)
                    RevisionBatches(iterator, count, result);
                else if (managed != null)
                {
                    while (WordNativeTiming.Active ? WordNativeTiming.Measure("Revision.MoveNext", managed.MoveNext) : managed.MoveNext())
                    {
                        result.Add(managed.Current);
                        if (result.Count > 10000)
                            throw new InvalidOperationException("Revision enumeration limit exceeded");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Revision enumeration unavailable");
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
                GC.KeepAlive(enumerator);
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
                if (count < 0 || count > 10000)
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
                if (!WordPasteOptions.IsWord(owner) || owner != Native.GetForegroundWindow() || WordSearchField.NativeFocused(owner))
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
            if (string.IsNullOrEmpty(input) || input.Length > 321 || !Core.SingleControlWord(input) && (input.Length > 160 || input.IndexOfAny(new[] { ' ', '\r', '\n', '\t' }) >= 0))
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

        static void RestoreFonts(FontSnapshot snapshot, object result, int start, Func<bool> context, VisibleMap verifiedMap)
        {
            object[] fonts = snapshot.Acquire();
            try
            {
                var map = verifiedMap;
                if (map == null || map.Start != start || map.Value != snapshot.Original)
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
            // Comment text and replies use wdCommentsStory (4). They retain the
            // same foreground, document, range, formatting and undo checks.
            return story >= 1 && story <= 4 || story >= 6 && story <= 11;
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
                    if (!WordPasteOptions.IsWord(owner) || owner != Native.GetForegroundWindow() || WordSearchField.NativeFocused(owner))
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

            // Called only after the native transaction verifies this selection.
            // It shares that request's projection, never a map from an earlier edit.
            internal static ReplayAnchor FromVerifiedSelection(IntPtr owner, string expected, object document, object scope, VisibleMap map, int kind, FontSnapshot continuation = null)
            {
                object font = null, tail = null;
                try
                {
                    int length = map == null ? -1 : SelectionFormatting.WordOwnedLength(expected, map.Value, map.Value.Length);
                    if (document == null || scope == null || length < 1 || !SupportedStory(kind))
                        return null;
                    int italic = int.MinValue;
                    if (continuation == null || !continuation.TryContinuation(document, map.Start, kind, out italic))
                    {
                        tail = Range(scope, map.NativeOffset(length - 1), map.NativeOffset(length));
                        font = Get(tail, "Font");
                        italic = Convert.ToInt32(Get(font, "Italic"));
                    }

                    Detail("continuation captured from transaction italic=" + italic + " length=" + length);
                    return new ReplayAnchor(owner, map.Start, kind, expected, document, italic, map);
                }
                catch
                {
                    return null;
                }
                finally
                {
                    Free(font);
                    Free(tail);
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
                if (!WordPasteOptions.IsWord(owner) || WordSearchField.NativeFocused(owner) || string.IsNullOrEmpty(expected) || expected.Length > 160 || expected.IndexOfAny(new[] { '\0', '\a' }) >= 0 || singleLetter && !SingleLetters.Supported(expected))
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
                if (WordSearchField.NativeFocused(owner))
                    return false;
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
                // Word can substitute a different font for an inserted
                // diacritic even in a one-letter edit of a named style.
                // Reapply the captured font for every nonempty replacement.
                if (patch.After.Length > 0)
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

        static bool LatinLetter(char value)
        {
            return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || char.IsLetter(value) && (value >= '\u00c0' && value <= '\u024f' || value >= '\u1e00' && value <= '\u1eff');
        }

        static bool BatchLetter(TextPatch patch)
        {
            return patch != null && patch.Before.Length == 1 && patch.After.Length == 1 && patch.Before != patch.After && LatinLetter(patch.Before[0]) && LatinLetter(patch.After[0]);
        }

        internal static int AdjacentPatches(TextPatch[] patches, int index)
        {
            int count = 1;
            if (!BatchLetter(patches[index]))
                return count;
            while (count < 32 && index + count < patches.Length && BatchLetter(patches[index + count]) && patches[index + count].Offset == patches[index + count - 1].Offset - 1)
                count++;
            return count;
        }

        // A short range export is a per-edit formatting/structure proof, not a
        // reusable permission. Unknown content keeps the existing letter edits.
        internal static bool PlainRun(string xml, string expected)
        {
            if (string.IsNullOrEmpty(xml) || xml.Length > 2097152)
                return false;
            try
            {
                const string word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                var doc = new System.Xml.XmlDocument
                {
                    XmlResolver = null
                };
                using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(xml), new System.Xml.XmlReaderSettings { XmlResolver = null, DtdProcessing = System.Xml.DtdProcessing.Prohibit, MaxCharactersInDocument = 2097152 }))
                    doc.Load(reader);
                var names = new System.Xml.XmlNamespaceManager(doc.NameTable);
                names.AddNamespace("w", word);
                var bodies = doc.SelectNodes("//w:document/w:body", names);
                if (bodies.Count != 1)
                    return false;
                System.Xml.XmlElement paragraph = null, run = null;
                foreach (System.Xml.XmlNode child in bodies[0].ChildNodes)
                {
                    if (child.NodeType != System.Xml.XmlNodeType.Element)
                        continue;
                    if (child.NamespaceURI != word)
                        return false;
                    if (child.LocalName == "sectPr")
                        continue;
                    if (child.LocalName != "p" || paragraph != null)
                        return false;
                    paragraph = (System.Xml.XmlElement)child;
                }

                if (paragraph == null)
                    return false;
                foreach (System.Xml.XmlNode child in paragraph.ChildNodes)
                {
                    if (child.NodeType != System.Xml.XmlNodeType.Element)
                        continue;
                    if (child.NamespaceURI != word)
                        return false;
                    if (child.LocalName == "pPr" || child.LocalName == "proofErr")
                        continue;
                    if (child.LocalName != "r" || run != null)
                    {
                        Detail("batch XML paragraph child=" + child.LocalName + " extraRun=" + (run != null));
                        return false;
                    }

                    run = (System.Xml.XmlElement)child;
                }

                if (run == null)
                    return false;
                var text = new StringBuilder();
                foreach (System.Xml.XmlNode child in run.ChildNodes)
                {
                    if (child.NodeType != System.Xml.XmlNodeType.Element)
                        continue;
                    if (child.NamespaceURI != word)
                        return false;
                    if (child.LocalName == "t")
                    {
                        foreach (System.Xml.XmlNode value in child.ChildNodes)
                            if (value.NodeType != System.Xml.XmlNodeType.Text && value.NodeType != System.Xml.XmlNodeType.Whitespace && value.NodeType != System.Xml.XmlNodeType.SignificantWhitespace)
                                return false;
                        text.Append(child.InnerText);
                    }
                    else if (child.LocalName == "rPr")
                    {
                        foreach (System.Xml.XmlNode property in child.ChildNodes)
                        {
                            if (property.NodeType != System.Xml.XmlNodeType.Element)
                                continue;
                            // Word's range export can add the built-in default
                            // character-style marker to otherwise plain text.
                            // Require its explicit default-character definition;
                            // actual named character styles keep letter edits.
                            if (property.NamespaceURI == word && property.LocalName == "rStyle" && !property.HasChildNodes && property.Attributes["val", word] != null)
                            {
                                var defaults = doc.SelectNodes("//w:styles/w:style[@w:type='character' and (@w:default='1' or @w:default='true')]", names);
                                if (defaults.Count == 1 && defaults[0].Attributes["styleId", word] != null && defaults[0].Attributes["styleId", word].Value == property.Attributes["val", word].Value && defaults[0].SelectSingleNode("w:rPr | w:basedOn | w:link", names) == null)
                                    continue;
                                Detail("batch XML default styles=" + defaults.Count + " referenced-default=" + (defaults.Count == 1 && defaults[0].Attributes["styleId", word] != null && defaults[0].Attributes["styleId", word].Value == property.Attributes["val", word].Value) + " styled=" + (defaults.Count == 1 && defaults[0].SelectSingleNode("w:rPr | w:basedOn | w:link", names) != null));
                            }

                            if (property.NamespaceURI != word || Array.IndexOf(new[] { "rFonts", "b", "bCs", "i", "iCs", "caps", "smallCaps", "strike", "dstrike", "color", "spacing", "w", "kern", "position", "sz", "szCs", "u", "vertAlign", "lang", "noProof" }, property.LocalName) < 0 || property.HasChildNodes)
                            {
                                Detail("batch XML property=" + property.LocalName);
                                return false;
                            }
                        }
                    }
                    else
                        return false;
                }

                return text.ToString() == expected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static bool MayBatchFormatting(object part)
        {
            object style = null, font = null;
            try
            {
                // Cheap rejection only. A named character style or mixed
                // font cannot pass PlainRun; avoid exporting its package.
                style = Get(part, "Style");
                if (style != null && !(style is string) && Convert.ToInt32(Get(style, "Type")) == 2)
                {
                    Detail("letter batch skipped character style");
                    return false;
                }

                font = Get(part, "Font");
                if (string.IsNullOrEmpty(Convert.ToString(Get(font, "Name"))))
                {
                    Detail("letter batch skipped mixed font");
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                // Missing optional metadata still requires the complete XML
                // proof; it never authorizes grouping by itself.
                return true;
            }
            finally
            {
                Free(font);
                Free(style);
            }
        }

        static TextPatch BatchPatch(object story, VisibleMap map, TextPatch[] patches, int index, int count, Func<bool> context)
        {
            int offset = patches[index + count - 1].Offset;
            var before = new StringBuilder();
            var after = new StringBuilder();
            for (int n = index + count - 1; n >= index; n--)
            {
                before.Append(patches[n].Before);
                after.Append(patches[n].After);
            }

            int[][] spans = map.Spans(offset, count);
            if (spans.Length != 1)
                return null;
            object part = null;
            bool plain = false;
            try
            {
                part = Range(story, spans[0][0], spans[0][1]);
                plain = MayBatchFormatting(part) && PlainRun(Convert.ToString(Get(part, "WordOpenXML")), before.ToString());
            }
            catch (Exception)
            {
            // Export is optional. Provider failures keep the established
            // single-character path; cancellation is checked outside here.
            }
            finally
            {
                Free(part);
            }

            if (!context())
                throw new InvalidOperationException("Conversion context changed during formatting proof");
            return plain ? new TextPatch(offset, before.ToString(), after.ToString()) : null;
        }

        internal static TextPatch[] PreparePatches(object whole, VisibleMap map, TextPatch[] patches, Func<bool> context, bool allowBatch)
        {
            if (!allowBatch || patches.Length < 2)
                return patches;
            int count = AdjacentPatches(patches, 0);
            if (count < 2)
                return patches;
            var batch = BatchPatch(whole, map, patches, 0, count, context);
            if (!context() || !map.MatchesRaw(Text(whole)) || Convert.ToInt32(Get(whole, "Start")) != map.Start)
                throw new InvalidOperationException("Batched selection moved or changed");
            if (batch == null)
            {
                Detail("letter batch declined count=" + count);
                return patches;
            }

            var result = new TextPatch[patches.Length - count + 1];
            result[0] = batch;
            Array.Copy(patches, count, result, 1, patches.Length - count);
            Detail("batched changed letters=" + count);
            return result;
        }

        internal static void ApplyPatches(object whole, object nativeStory, ref VisibleMap currentMap, TextPatch[] patches, Func<bool> context, ref bool changed, ref string currentText, ref int end, ref int lastStoryEnd)
        {
            int start = currentMap.Start;
            for (int index = 0; index < patches.Length; index++)
            {
                var beforePatch = currentMap;
                if (beforePatch == null || beforePatch.Value != currentText || !beforePatch.MatchesRaw(Text(whole)) || Convert.ToInt32(Get(whole, "Start")) != start)
                    throw new InvalidOperationException("Selection moved or changed");
                if (!context())
                    throw new InvalidOperationException("Conversion context changed");
                TextPatch patch = patches[index];
                int storyEndBefore = Convert.ToInt32(Get(nativeStory, "End"));
                var insertedMap = ReplacePatch(whole, beforePatch, patch, ref changed);
                currentText = currentText.Remove(patch.Offset, patch.Length).Insert(patch.Offset, patch.After);
                // Growth includes retained tracked deletions. Keep the same
                // fresh inserted projection and story bounds as letter edits.
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

        internal static bool ApplyItalic(object portion, object font, ItalicSpan span)
        {
            if (!span.Force && (Convert.ToInt32(Get(font, "Bold")) == -1 || DisplayStyle(portion)))
                return false;
            int wanted = span.Italic ? -1 : 0;
            if (Convert.ToInt32(Get(font, "Italic")) == wanted)
                return false;
            Set(font, "Italic", wanted);
            return true;
        }

        public static bool Replace(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics = null, bool retain = true, string balaramWordFont = null, FontSnapshot restoreFonts = null, Action<FontSnapshot> captureFonts = null, ItalicSpan[] fontSpans = null, ReplayAnchor replay = null, FontSnapshot continuationFonts = null, Action<ReplayAnchor> captureReplay = null)
        {
            return ReplaceCore(owner, input, output, context, out handled, italics, retain, balaramWordFont, restoreFonts, captureFonts, fontSpans, false, replay, continuationFonts, captureReplay);
        }

        public static bool ReplaceExpansion(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics = null, bool retain = false, ReplayAnchor replay = null, Action<ReplayAnchor> captureReplay = null)
        {
            return ReplaceCore(owner, input, output, context, out handled, italics, retain, null, null, null, null, true, replay, null, captureReplay);
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
                    if (previous.Italic && span.Italic && previous.Force && span.Force && gap)
                    {
                        merged[merged.Count - 1] = new ItalicSpan(previous.Start, span.Start + span.Length - previous.Start, true);
                        continue;
                    }
                }

                merged.Add(span);
            }

            return merged.ToArray();
        }

        static bool ReplaceCore(IntPtr owner, string input, string output, Func<bool> context, out bool handled, ItalicSpan[] italics, bool retain, string balaramWordFont, FontSnapshot restoreFonts, Action<FontSnapshot> captureFonts, ItalicSpan[] fontSpans, bool expansion, ReplayAnchor replay, FontSnapshot continuationFonts, Action<ReplayAnchor> captureReplay)
        {
            handled = WordPasteOptions.IsWord(owner);
            if (!handled)
                return false;
            using (WordNativeTiming.Begin(Trace))
            {
                handled = !WordPasteOptions.TextFieldFocused(owner);
                if (!handled)
                    return false;
                if (!WordPasteOptions.DocumentFocused(owner))
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
                    WordNativeTiming.Next("bind");
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
                    WordNativeTiming.Next("range");
                    selection = Get(app, "Selection");
                    selectionRange = Get(selection, "Range");
                    int storyType = Convert.ToInt32(Get(selectionRange, "StoryType"));
                    if (!SupportedStory(storyType))
                        return false;
                    document = Get(selection, "Document");
                    int start = Convert.ToInt32(Get(selection, "Start")), end = Convert.ToInt32(Get(selection, "End"));
                    int selectedEnd = end;
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
                    // WordOpenXML can interfere with an open custom undo record.
                    // Prove only the first candidate before any edit/undo begins;
                    // later runs retain their original per-character changes.
                    if (storyType == 1 && !expansion && patches.Length > 1 && AdjacentPatches(patches, 0) > 1)
                    {
                        undo = Get(app, "UndoRecord");
                        if (Convert.ToInt32(Get(undo, "CustomRecordLevel")) != 0)
                            return false;
                        WordNativeTiming.Next("batch-proof");
                        patches = PreparePatches(whole, initialMap, patches, context, true);
                        if (!context() || !OwnedTextAnchor.Foreground(app, owner) || Convert.ToInt32(Get(selection, "Start")) != start || Convert.ToInt32(Get(selection, "End")) != selectedEnd)
                            throw new InvalidOperationException("Selection changed during formatting proof");
                    }

                    WordNativeTiming.Next("font-snapshot");
                    if (captureFonts != null)
                        captured = CaptureFonts(document, whole, start, input, initialMap);
                    WordNativeTiming.Next("replay-capture");
                    if (replay == null && captureReplay != null)
                    {
                        replay = ReplayAnchor.FromVerifiedSelection(owner, input, document, whole, initialMap, storyType, continuationFonts ?? captured);
                        if (replay != null)
                            captureReplay(replay);
                    }

                    WordNativeTiming.Next("undo-start");
                    object typingFont = Get(selection, "Font");
                    objects.Add(typingFont);
                    typingItalic = Convert.ToInt32(Get(typingFont, "Italic"));
                    int originalTypingItalic;
                    if (continuationFonts != null && continuationFonts.TryContinuation(document, start, storyType, out originalTypingItalic))
                        typingItalic = originalTypingItalic;
                    if (undo == null)
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
                    WordNativeTiming.Next("patches");
                    ApplyPatches(whole, nativeStory, ref currentMap, patches, context, ref changed, ref currentText, ref end, ref lastStoryEnd);
                    WordNativeTiming.Next("formatting");
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
                                    if (ApplyItalic(portion, font, span))
                                        changed = true;
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
                        RestoreFonts(restoreFonts, result, start, context, resultMap);
                        changed = true;
                    }

                    WordNativeTiming.Next("selection-end");
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

                    WordNativeTiming.Next("undo-end");
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

                    WordNativeTiming.Committed();
                    return true;
                }
                catch (Exception ex)
                {
                    WordNativeTiming.Next("rollback");
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
                    WordNativeTiming.Next("cleanup");
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
}
