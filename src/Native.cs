// KeyJaya: Windows interop, input-event construction/replay, WebView focus, and clipboard ownership.
// Input tags and sequence checks distinguish physical input from KeyJaya's own temporary events/data.
using System.Linq;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyJaya
{
    // WinUI can report its host pane as focused even when a WebView editor has
    // keyboard focus. Resolve only WhatsApp's own embedded browser; never infer a
    // target from the application name, a cached caret, or text elsewhere on screen.
    public static class EmbeddedWebViewFocus
    {
        sealed class Renderer
        {
            public IntPtr Window;
            public uint Process;
        }

        static readonly object gate = new object ();
        static IntPtr cachedOwner;
        static uint cachedProcess;
        static DateTime cachedUntil;
        static Renderer[] cached = new Renderer[0];
        public static Action<string> Trace;
        delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumWindows(WindowVisitor callback, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr window, WindowVisitor callback, IntPtr parameter);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct ProcessEntry
        {
            public uint Size, Usage, Process;
            public UIntPtr Heap;
            public uint Module, Threads, Parent;
            public int Priority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string Exe;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint process);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        public static bool DescendsFrom(uint process, uint owner, System.Collections.Generic.IDictionary<uint, uint> parents)
        {
            if (process == 0 || owner == 0 || process == owner || parents == null)
                return false;
            var visited = new System.Collections.Generic.HashSet<uint>();
            for (int n = 0; n < 12 && process != 0 && visited.Add(process); n++)
            {
                uint parent;
                if (!parents.TryGetValue(process, out parent))
                    return false;
                if (parent == owner)
                    return true;
                process = parent;
            }

            return false;
        }

        static System.Collections.Generic.Dictionary<uint, uint> Parents()
        {
            var result = new System.Collections.Generic.Dictionary<uint, uint>();
            IntPtr snapshot = CreateToolhelp32Snapshot(2, 0);
            if (snapshot == new IntPtr(-1))
                return result;
            try
            {
                var entry = new ProcessEntry
                {
                    Size = (uint)Marshal.SizeOf(typeof(ProcessEntry))
                };
                if (Process32FirstW(snapshot, ref entry))
                    do
                    {
                        result[entry.Process] = entry.Parent;
                    }
                    while (Process32NextW(snapshot, ref entry));
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return result;
        }

        static string Class(IntPtr window)
        {
            var name = new StringBuilder(128);
            Native.GetClassName(window, name, name.Capacity);
            return name.ToString();
        }

        static Renderer[] Renderers(IntPtr owner, uint pid)
        {
            lock (gate)
            {
                if (cachedOwner == owner && cachedProcess == pid && DateTime.UtcNow < cachedUntil)
                    return cached;
                var parents = Parents();
                var found = new System.Collections.Generic.List<Renderer>();
                WindowVisitor visit = (window, unused) =>
                {
                    uint child;
                    Native.GetWindowThreadProcessId(window, out child);
                    if (Class(window) != "Chrome_WidgetWin_1" || !DescendsFrom(child, pid, parents) || !Native.ProcessImageIs(child, "msedgewebview2.exe"))
                        return true;
                    WindowVisitor descendants = (renderer, ignored) =>
                    {
                        uint rendererPid;
                        Native.GetWindowThreadProcessId(renderer, out rendererPid);
                        if (Class(renderer) == "Chrome_RenderWidgetHostHWND" && DescendsFrom(rendererPid, pid, parents) && Native.ProcessImageIs(rendererPid, "msedgewebview2.exe"))
                            found.Add(new Renderer { Window = renderer, Process = rendererPid });
                        return found.Count < 16;
                    };
                    EnumChildWindows(window, descendants, IntPtr.Zero);
                    return found.Count < 16;
                };
                EnumWindows(visit, IntPtr.Zero);
                cachedOwner = owner;
                cachedProcess = pid;
                cachedUntil = DateTime.UtcNow.AddSeconds(found.Count > 0 ? 5 : 0.25);
                return cached = found.ToArray();
            }
        }

        public static System.Windows.Automation.AutomationElement Focused(IntPtr owner)
        {
            var normal = System.Windows.Automation.AutomationElement.FocusedElement;
            if (owner == IntPtr.Zero || owner != Native.GetForegroundWindow() || Class(owner) != "WinUIDesktopWin32WindowClass")
                return normal;
            uint pid;
            Native.GetWindowThreadProcessId(owner, out pid);
            if (!Native.ProcessImageIs(pid, "WhatsApp.Root.exe") && !Native.ProcessImageIs(pid, "WhatsApp.exe"))
                return normal;
            try
            {
                object provider;
                if (normal != null && normal.TryGetCurrentPattern(System.Windows.Automation.TextPattern.Pattern, out provider))
                    return normal;
                var identity = Native.FocusIdentity();
                var condition = new System.Windows.Automation.AndCondition(new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.HasKeyboardFocusProperty, true), new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.IsTextPatternAvailableProperty, true), new System.Windows.Automation.PropertyCondition(System.Windows.Automation.AutomationElement.IsPasswordProperty, false));
                foreach (var renderer in Renderers(owner, pid))
                {
                    uint actual;
                    Native.GetWindowThreadProcessId(renderer.Window, out actual);
                    if (actual != renderer.Process || Class(renderer.Window) != "Chrome_RenderWidgetHostHWND")
                        continue;
                    var root = System.Windows.Automation.AutomationElement.FromHandle(renderer.Window);
                    var focused = root.FindFirst(System.Windows.Automation.TreeScope.Descendants, condition);
                    if (focused == null || focused.Current.ProcessId != actual || !focused.Current.HasKeyboardFocus)
                        continue;
                    if (owner != Native.GetForegroundWindow() || !EditGuard.SameFocus(identity, Native.FocusIdentity()))
                        return null;
                    if (Trace != null)
                        Trace("WhatsApp embedded editor focus resolved");
                    return focused;
                }
            }
            catch (Exception ex)
            {
                if (Trace != null)
                    Trace("WhatsApp embedded editor unavailable: " + ex.GetType().Name);
            }

            return normal;
        }
    }

    public static class Native
    {
        public const uint OwnInput = 0x4B4A164, ReplayedInput = 0x4B4A165, OwnCorrectionInput = 0x4B4A166, DrainedInput = 0x4B4A167;
        public delegate IntPtr Hook(int code, IntPtr w, IntPtr l);
        public delegate void WinEvent(IntPtr hook, uint evt, IntPtr hwnd, int obj, int child, uint thread, uint time);
        [StructLayout(LayoutKind.Sequential)]
        public struct KeyData
        {
            public uint vk, scan, flags, time;
            public UIntPtr extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardInput
        {
            public ushort vk, scan;
            public uint flags, time;
            public UIntPtr extra;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KeyboardInput keyboard;
            [FieldOffset(0)]
            public MouseInput mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int dx, dy;
            public uint data, flags, time;
            public UIntPtr extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public uint type;
            public InputUnion union;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int kind, Hook callback, IntPtr module, uint thread);
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr window, uint flags);
        public delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint thread, WindowVisitor visitor, IntPtr parameter);
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")]
        public static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr window);
        static bool OpenClipboardBriefly(IntPtr window)
        {
            for (int n = 0; n < 6; n++)
            {
                if (OpenClipboard(window))
                    return true;
                if (n < 5)
                    System.Threading.Thread.Sleep(5);
            }

            return false;
        }

        [DllImport("user32.dll")]
        static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardOwner();
        [DllImport("user32.dll")]
        static extern uint EnumClipboardFormats(uint format);
        static bool ClipboardActuallyEmpty(uint sequence)
        {
            if (!OpenClipboardBriefly(IntPtr.Zero))
                return false;
            try
            {
                return sequence == GetClipboardSequenceNumber() && EnumClipboardFormats(0) == 0;
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static IntPtr ClipboardOwner()
        {
            return GetClipboardOwner();
        }

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint format, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern uint RegisterClipboardFormat(string format);
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr handle);
        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalFree(IntPtr handle);
        static bool ClipboardData(uint format, byte[] bytes)
        {
            IntPtr handle = GlobalAlloc(2, new UIntPtr((uint)bytes.Length));
            if (handle == IntPtr.Zero)
                return false;
            IntPtr memory = GlobalLock(handle);
            if (memory == IntPtr.Zero)
            {
                GlobalFree(handle);
                return false;
            }

            Marshal.Copy(bytes, 0, memory, bytes.Length);
            GlobalUnlock(handle);
            if (SetClipboardData(format, handle) != IntPtr.Zero)
                return true;
            GlobalFree(handle);
            return false;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint format);
        [DllImport("kernel32.dll")]
        static extern UIntPtr GlobalSize(IntPtr handle);
        [DllImport("ole32.dll")]
        static extern int OleGetClipboard(out System.Runtime.InteropServices.ComTypes.IDataObject value);
        [DllImport("ole32.dll")]
        static extern int OleSetClipboard(System.Runtime.InteropServices.ComTypes.IDataObject value);
        [DllImport("ole32.dll")]
        static extern int OleIsCurrentClipboard(System.Runtime.InteropServices.ComTypes.IDataObject value);
        [DllImport("ole32.dll")]
        static extern int OleFlushClipboard();
        [DllImport("ole32.dll")]
        static extern void ReleaseStgMedium(ref System.Runtime.InteropServices.ComTypes.STGMEDIUM value);
        // Adobe sometimes advertises its interchange format after copying plain
        // dialog text, but the native provider returns CLIPBRD_E_BAD_DATA. There is
        // no payload to preserve in that case. A real or merely inaccessible Adobe
        // payload must still make the whole backup fail rather than be discarded.
        static bool EmptyAdobeClipboardAdvertisement(string format)
        {
            if (format != "InDesignCS3Interchange")
                return false;
            System.Runtime.InteropServices.ComTypes.IDataObject source = null;
            System.Runtime.InteropServices.ComTypes.IEnumFORMATETC entries = null;
            try
            {
                if (OleGetClipboard(out source) < 0 || source == null)
                    return false;
                entries = source.EnumFormatEtc(System.Runtime.InteropServices.ComTypes.DATADIR.DATADIR_GET);
                var row = new System.Runtime.InteropServices.ComTypes.FORMATETC[1];
                var fetched = new int[1];
                int id = System.Windows.Forms.DataFormats.GetFormat(format).Id;
                bool found = false;
                for (int n = 0; n < 128 && entries.Next(1, row, fetched) == 0; n++)
                {
                    var item = row[0];
                    try
                    {
                        if ((ushort)item.cfFormat != id)
                            continue;
                        foreach (int bit in new[]
                        {
                            1,
                            2,
                            4,
                            8,
                            16,
                            32,
                            64
                        }

                        )
                        {
                            var requested = (System.Runtime.InteropServices.ComTypes.TYMED)bit;
                            if ((item.tymed & requested) == 0)
                                continue;
                            found = true;
                            var single = item;
                            single.tymed = requested;
                            System.Runtime.InteropServices.ComTypes.STGMEDIUM medium;
                            try
                            {
                                source.GetData(ref single, out medium);
                            }
                            catch (COMException ex)
                            {
                                if (ex.HResult != unchecked((int)0x800401D3))
                                    return false;
                                continue;
                            }

                            try
                            {
                                return false;
                            }
                            finally
                            {
                                ReleaseStgMedium(ref medium);
                            }
                        }
                    }
                    finally
                    {
                        if (item.ptd != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(item.ptd);
                    }
                }

                return found;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (entries != null && Marshal.IsComObject(entries))
                    Marshal.ReleaseComObject(entries);
                if (source != null && Marshal.IsComObject(source))
                    Marshal.ReleaseComObject(source);
            }
        }

        public sealed class ClipboardReference : IDisposable
        {
            System.Windows.Forms.DataObject original;
            public readonly uint Sequence;
            [ThreadStatic]
            public static string LastFailure;
            ClipboardReference(System.Windows.Forms.DataObject value, uint sequence)
            {
                original = value;
                Sequence = sequence;
            }

            // An OLE clipboard proxy is not an immutable snapshot. Materialize every
            // advertised format before changing the clipboard, or decline the probe.
            public static ClipboardReference Capture()
            {
                for (int n = 0; n < 3; n++)
                {
                    if (n > 0 && System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
                        System.Windows.Forms.Application.DoEvents();
                    var value = CaptureOnce();
                    if (value != null)
                        return value;
                    if (n < 2)
                        System.Threading.Thread.Sleep(5);
                }

                return null;
            }

            static ClipboardReference CaptureOnce()
            {
                LastFailure = null;
                try
                {
                    uint sequence = GetClipboardSequenceNumber();
                    var value = System.Windows.Forms.Clipboard.GetDataObject();
                    var copy = new System.Windows.Forms.DataObject();
                    string[] formats = value == null ? new string[0] : value.GetFormats(false);
                    // OLE may transiently return no object/formats while native data is
                    // still present. That is a failed read, never an empty backup.
                    if (formats.Length == 0 && !ClipboardActuallyEmpty(sequence))
                    {
                        LastFailure = "OLE formats unavailable for nonempty clipboard";
                        return null;
                    }

                    foreach (string format in formats)
                    {
                        object data = value.GetData(format, false);
                        if (data == null)
                        {
                            if (EmptyAdobeClipboardAdvertisement(format))
                                continue;
                            LastFailure = "unavailable format: " + format;
                            return null;
                        }

                        copy.SetData(format, false, SelectionConversion.CloneClipboardValue(data));
                    }

                    if (sequence != GetClipboardSequenceNumber())
                    {
                        LastFailure = "sequence changed during snapshot";
                        return null;
                    }

                    string raw;
                    bool readable = ClipboardUnicode(sequence, 8 * 1024 * 1024, out raw);
                    bool unicode = copy.GetDataPresent(System.Windows.Forms.DataFormats.UnicodeText, false);
                    if (readable ? (!unicode || copy.GetData(System.Windows.Forms.DataFormats.UnicodeText, false) as string != raw) : (unicode || ClipboardReadFailure != "Unicode payload unavailable"))
                    {
                        LastFailure = "native Unicode snapshot verification failed: " + ClipboardReadFailure;
                        return null;
                    }

                    if (sequence != GetClipboardSequenceNumber())
                    {
                        LastFailure = "sequence changed during snapshot verification";
                        return null;
                    }

                    return new ClipboardReference(copy, sequence);
                }
                catch (Exception ex)
                {
                    LastFailure = ex.GetType().Name + " code=" + ex.HResult;
                    return null;
                }
            }

            public bool Restore(uint sequence)
            {
                LastFailure = null;
                if (original == null)
                {
                    LastFailure = "snapshot disposed";
                    return false;
                }

                var data = (System.Runtime.InteropServices.ComTypes.IDataObject)original;
                int result = 0;
                bool installed = false;
                // Retry only while the expected clipboard is unchanged. Once set,
                // retry flushing this exact OLE object, never setting it again over
                // a newer user copy. Flushing makes it survive the probe's disposal.
                for (int n = 0; n < 12; n++)
                {
                    if (!installed)
                    {
                        if (sequence != GetClipboardSequenceNumber())
                        {
                            LastFailure = "clipboard sequence changed before restore";
                            return false;
                        }

                        result = OleSetClipboard(data);
                        installed = result >= 0;
                    }

                    if (installed)
                    {
                        if (OleIsCurrentClipboard(data) != 0)
                        {
                            LastFailure = "clipboard changed before flush";
                            return false;
                        }

                        result = OleFlushClipboard();
                        if (result >= 0)
                            return true;
                    }

                    if (result != unchecked((int)0x800401D0) && result != unchecked((int)0x800401D4))
                        break;
                    if (n < 11)
                    {
                        System.Windows.Forms.Application.DoEvents();
                        System.Threading.Thread.Sleep(5);
                    }
                }

                LastFailure = "restore HRESULT=" + result;
                return false;
            }

            public void Dispose()
            {
                original = null;
            }
        }

        public static byte[] ClipboardFormatBytes(uint format)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return null;
            try
            {
                IntPtr handle = GetClipboardData(format);
                if (handle == IntPtr.Zero)
                    return null;
                ulong size = GlobalSize(handle).ToUInt64();
                if (size == 0 || size > 16 * 1024 * 1024)
                    return null;
                IntPtr memory = GlobalLock(handle);
                if (memory == IntPtr.Zero)
                    return null;
                try
                {
                    byte[] bytes = new byte[(int)size];
                    Marshal.Copy(memory, bytes, 0, bytes.Length);
                    return bytes;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        [ThreadStatic]
        public static string ClipboardReadFailure;
        public static bool ClipboardUnicode(uint sequence, int limit, out string value)
        {
            value = null;
            ClipboardReadFailure = null;
            if (limit < 0 || limit > 8 * 1024 * 1024 || sequence != GetClipboardSequenceNumber())
            {
                ClipboardReadFailure = "stale request";
                return false;
            }

            if (!OpenClipboardBriefly(IntPtr.Zero))
            {
                ClipboardReadFailure = "clipboard busy";
                return false;
            }

            try
            {
                if (sequence != GetClipboardSequenceNumber())
                {
                    ClipboardReadFailure = "sequence changed before read";
                    return false;
                }

                IntPtr handle = GetClipboardData(13);
                if (handle == IntPtr.Zero)
                {
                    ClipboardReadFailure = "Unicode payload unavailable";
                    return false;
                }

                ulong bytes = GlobalSize(handle).ToUInt64();
                if (bytes < 2 || bytes % 2 != 0)
                {
                    ClipboardReadFailure = "invalid Unicode allocation";
                    return false;
                }

                IntPtr memory = GlobalLock(handle);
                if (memory == IntPtr.Zero)
                {
                    ClipboardReadFailure = "Unicode allocation locked";
                    return false;
                }

                try
                {
                    string text = Marshal.PtrToStringUni(memory, (int)Math.Min(bytes / 2, (ulong)limit + 1));
                    int end = text.IndexOf('\0');
                    if (end < 0 || end > limit)
                    {
                        ClipboardReadFailure = "Unicode terminator outside bound";
                        return false;
                    }

                    value = text.Substring(0, end);
                    if (sequence != GetClipboardSequenceNumber())
                    {
                        ClipboardReadFailure = "sequence changed after read";
                        return false;
                    }

                    return true;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        static IntPtr clipboardFailureWindow;
        static uint clipboardFailureSequence;
        static IntPtr clipboardLeaseWindow;
        static uint clipboardLeaseSequence;
        static byte[] clipboardLeaseToken, clipboardLeaseText, clipboardLeaseHtml;
        static uint LeaseFormat()
        {
            return RegisterClipboardFormat("KeyJaya.TemporaryPayload");
        }

        static bool ClipboardBytesMatch(uint format, byte[] expected)
        {
            if (format == 0 || expected == null)
                return false;
            IntPtr handle = GetClipboardData(format);
            if (handle == IntPtr.Zero || GlobalSize(handle).ToUInt64() < (ulong)expected.Length)
                return false;
            IntPtr memory = GlobalLock(handle);
            if (memory == IntPtr.Zero)
                return false;
            try
            {
                byte[] actual = new byte[expected.Length];
                Marshal.Copy(memory, actual, 0, actual.Length);
                for (int n = 0; n < actual.Length; n++)
                    if (actual[n] != expected[n])
                        return false;
                return true;
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }

        static bool OwnLeaseOpen(IntPtr window, uint written)
        {
            if (window != clipboardLeaseWindow || written != clipboardLeaseSequence || GetClipboardOwner() != window || clipboardLeaseToken == null)
                return false;
            return ClipboardBytesMatch(LeaseFormat(), clipboardLeaseToken) && ClipboardBytesMatch(13, clipboardLeaseText) && (clipboardLeaseHtml == null || ClipboardBytesMatch(RegisterClipboardFormat("HTML Format"), clipboardLeaseHtml));
        }

        public static bool TemporaryClipboardOwned(IntPtr window, uint written)
        {
            if (!OpenClipboardBriefly(window))
                return false;
            try
            {
                return OwnLeaseOpen(window, written);
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static bool FailedTemporaryClipboardOwned(IntPtr window, uint written)
        {
            return clipboardFailureWindow == window && clipboardFailureSequence == written && GetClipboardOwner() == window && GetClipboardSequenceNumber() == written;
        }

        public static void ReleaseTemporaryClipboard(IntPtr window, uint written)
        {
            if (window == clipboardFailureWindow && written == clipboardFailureSequence)
                clipboardFailureWindow = IntPtr.Zero;
            if (window == clipboardLeaseWindow && written == clipboardLeaseSequence)
            {
                clipboardLeaseToken = clipboardLeaseText = clipboardLeaseHtml = null;
                clipboardLeaseWindow = IntPtr.Zero;
            }
        }

        public static bool TemporaryClipboard(IntPtr window, string value, out uint sequence, byte[] html = null)
        {
            return WriteTemporaryClipboard(window, value, out sequence, html, null);
        }

        public static bool ExpectedTemporaryClipboard(IntPtr window, string value, uint expected, out uint sequence, byte[] html = null)
        {
            return WriteTemporaryClipboard(window, value, out sequence, html, expected);
        }

        [ThreadStatic]
        public static string ClipboardWriteFailure;
        static bool WriteTemporaryClipboard(IntPtr window, string value, out uint sequence, byte[] html, uint? expected)
        {
            sequence = 0;
            ClipboardWriteFailure = null;
            if (!(expected.HasValue ? OpenClipboardBriefly(window) : OpenClipboard(window)))
            {
                ClipboardWriteFailure = "clipboard busy";
                return false;
            }

            if (expected.HasValue && GetClipboardSequenceNumber() != expected.Value)
            {
                ClipboardWriteFailure = "clipboard changed before write";
                CloseClipboard();
                return false;
            }

            byte[] token = Guid.NewGuid().ToByteArray(), unicode = System.Text.Encoding.Unicode.GetBytes(value + "\0");
            bool written = false, emptied = false;
            try
            {
                if (!EmptyClipboard())
                {
                    ClipboardWriteFailure = "empty failed";
                    return false;
                }

                emptied = true;
                foreach (string format in new[]
                {
                    "ExcludeClipboardContentFromMonitorProcessing",
                    "CanIncludeInClipboardHistory",
                    "CanUploadToCloudClipboard"
                }

                )
                {
                    uint id = RegisterClipboardFormat(format);
                    if (id == 0 || !ClipboardData(id, new byte[4]))
                    {
                        ClipboardWriteFailure = "metadata write failed";
                        EmptyClipboard();
                        return false;
                    }
                }

                if (!ClipboardData(LeaseFormat(), token) || !ClipboardData(13, unicode))
                {
                    ClipboardWriteFailure = "text write failed";
                    EmptyClipboard();
                    return false;
                }

                if (html != null)
                {
                    uint format = RegisterClipboardFormat("HTML Format");
                    if (format == 0 || !ClipboardData(format, html))
                    {
                        EmptyClipboard();
                        return false;
                    }
                }

                written = true;
            }
            finally
            {
                CloseClipboard();
                if (emptied && !written)
                {
                    clipboardFailureWindow = window;
                    clipboardFailureSequence = GetClipboardSequenceNumber();
                }
            }

            if (!written)
                return false;
            sequence = GetClipboardSequenceNumber();
            clipboardFailureWindow = IntPtr.Zero;
            clipboardLeaseWindow = window;
            clipboardLeaseSequence = sequence;
            clipboardLeaseToken = token;
            clipboardLeaseText = unicode;
            clipboardLeaseHtml = html;
            return true;
        }

        public static bool ClearTemporaryClipboard(IntPtr window, uint expected, out uint sequence)
        {
            sequence = expected;
            if (!OpenClipboardBriefly(window))
                return false;
            bool cleared = false;
            try
            {
                if (!OwnLeaseOpen(window, expected))
                    return false;
                if (!EmptyClipboard())
                    return false;
                ReleaseTemporaryClipboard(window, expected);
                cleared = true;
            }
            finally
            {
                CloseClipboard();
            }

            if (cleared)
                sequence = GetClipboardSequenceNumber();
            return cleared;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseHookData
        {
            public Point point;
            public uint data, flags, time;
            public UIntPtr extra;
        }

        /// <summary>
        /// Holds physical events for one foreground owner while an edit is in flight.
        /// Deferred and in-flight events are separate so a replayed shortcut can
        /// start another conversion without losing or delivering an event twice.
        /// </summary>
        public sealed class CorrectionInputHold
        {
            readonly object gate = new object ();
            readonly System.Collections.Generic.Queue<Input> queued = new System.Collections.Generic.Queue<Input>();
            Input? deferred, inFlight;
            IntPtr owner;
            bool active;
            readonly System.Collections.Generic.HashSet<uint> initialReleases = new System.Collections.Generic.HashSet<uint>();
            readonly System.Collections.Generic.HashSet<uint> heldModifiers = new System.Collections.Generic.HashSet<uint>();
            public bool Active
            {
                get
                {
                    lock (gate)
                        return active;
                }
            }

            public int Count
            {
                get
                {
                    lock (gate)
                        return queued.Count + (deferred.HasValue ? 1 : 0);
                }
            }

            public bool FinishIfEmpty(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || owner != window || queued.Count != 0 || deferred.HasValue || inFlight.HasValue)
                        return false;
                    active = false;
                    owner = IntPtr.Zero;
                    initialReleases.Clear();
                    heldModifiers.Clear();
                    return true;
                }
            }

            // Failed discovery may accumulate more bare Ctrl taps. They contain no
            // text and cannot repair an unavailable target. Retire only complete
            // Ctrl down/up pairs; never discard typing, chords, mouse input, or a
            // release whose corresponding down may already have reached Windows.
            public bool FinishIfOnlyControlTaps(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || owner != window || inFlight.HasValue || heldModifiers.Count != 0)
                        return false;
                    var keys = deferred.HasValue ? new[]
                    {
                        deferred.Value
                    }.Concat(queued) : queued.AsEnumerable();
                    var pressed = new System.Collections.Generic.HashSet<uint>();
                    bool any = false;
                    foreach (Input item in keys)
                    {
                        if (item.type != 1)
                            return false;
                        var key = item.union.keyboard;
                        uint vk = key.vk == 0x11 ? 0xA2u : key.vk;
                        if (vk != 0xA2 && vk != 0xA3)
                            return false;
                        bool up = (key.flags & 2) != 0;
                        if (up)
                        {
                            if (!pressed.Remove(vk))
                                return false;
                        }
                        else if (!pressed.Add(vk))
                            return false;
                        any = true;
                    }

                    if (!any || pressed.Count != 0)
                        return false;
                    queued.Clear();
                    deferred = null;
                    active = false;
                    owner = IntPtr.Zero;
                    initialReleases.Clear();
                    return true;
                }
            }

            public bool Owns(IntPtr window)
            {
                lock (gate)
                    return active && owner == window;
            }

            public bool Begin(IntPtr window)
            {
                lock (gate)
                {
                    if (active)
                        return false;
                    owner = window;
                    active = true;
                    queued.Clear();
                    initialReleases.Clear();
                    heldModifiers.Clear();
                    deferred = inFlight = null;
                    return true;
                }
            }

            // Releases belonging to the initiating shortcut must reach its dispatcher
            // and Windows before conversion waits for modifiers. Later chords stay FIFO.
            public void InitialRelease(uint key)
            {
                lock (gate)
                    if (active)
                        initialReleases.Add(key);
            }

            public bool HasTyping
            {
                get
                {
                    lock (gate)
                    {
                        bool control = false, alt = false, win = false;
                        foreach (Input item in (deferred.HasValue ? new[]
                        {
                            deferred.Value
                        }.Concat(queued) : queued.AsEnumerable()))
                        {
                            if (item.type != 1)
                                continue;
                            var key = item.union.keyboard;
                            bool down = (key.flags & 2) == 0;
                            uint vk = key.vk;
                            if (vk == 0x11 || vk == 0xA2 || vk == 0xA3)
                            {
                                control = down;
                                continue;
                            }

                            if (vk == 0x12 || vk == 0xA4 || vk == 0xA5)
                            {
                                alt = down;
                                continue;
                            }

                            if (vk == 0x5B || vk == 0x5C)
                            {
                                win = down;
                                continue;
                            }

                            if (down && !control && !alt && !win && (vk == 13 || vk == 9 || vk == 32 || vk == 0xE7 || vk >= 0x30 && vk <= 0x5A || vk >= 0x60 && vk <= 0x6F || vk >= 0xBA && vk <= 0xE2))
                                return true;
                        }

                        return false;
                    }
                }
            }

            static uint Modifier(uint key)
            {
                return key == 0x10 ? 0xA0u : key == 0x11 ? 0xA2u : key == 0x12 ? 0xA4u : key >= 0xA0 && key <= 0xA5 || key == 0x5B || key == 0x5C ? key : 0;
            }

            static Input HeldKey(KeyData key)
            {
                return new Input
                {
                    type = 1,
                    union = new InputUnion
                    {
                        keyboard = new KeyboardInput
                        {
                            vk = (ushort)key.vk,
                            scan = (ushort)key.scan,
                            flags = ((key.flags & 1) != 0 ? 1u : 0u) | ((key.flags & 0x80) != 0 ? 2u : 0u),
                            extra = new UIntPtr(ReplayedInput)
                        }
                    }
                };
            }

            public bool Keyboard(KeyData key, int message, IntPtr window)
            {
                lock (gate)
                {
                    if (!active || key.extra.ToUInt64() == OwnInput)
                        return false;
                    uint modifier = Modifier(key.vk);
                    bool up = (key.flags & 0x80) != 0;
                    // A captured modifier-down may be replayed after focus returns. Its
                    // physical release must also be in that replay, even if the release
                    // occurred in another window. Let that real release pass through.
                    if (owner != window)
                    {
                        if (up && modifier != 0 && heldModifiers.Remove(modifier))
                            queued.Enqueue(HeldKey(key));
                        return false;
                    }

                    if (initialReleases.Contains(key.vk))
                    {
                        if (up)
                            initialReleases.Remove(key.vk);
                        return false;
                    }

                    if (modifier != 0)
                    {
                        if (up)
                            heldModifiers.Remove(modifier);
                        else
                            heldModifiers.Add(modifier);
                    }

                    queued.Enqueue(HeldKey(key));
                    return true;
                }
            }

            public bool Mouse(MouseHookData key, int message, IntPtr window)
            {
                uint flags = message == 0x201 ? 2u : message == 0x202 ? 4u : message == 0x204 ? 8u : message == 0x205 ? 16u : message == 0x207 ? 32u : message == 0x208 ? 64u : message == 0x20A ? 0x800u : message == 0x20E ? 0x1000u : message == 0x20B ? 0x80u : message == 0x20C ? 0x100u : 0u;
                if (flags == 0)
                    return false;
                lock (gate)
                {
                    if (!active || owner != window || key.extra.ToUInt64() == OwnInput)
                        return false;
                    var screen = System.Windows.Forms.SystemInformation.VirtualScreen;
                    int x = (int)Math.Round((key.point.X - screen.Left) * 65535.0 / Math.Max(1, screen.Width - 1)), y = (int)Math.Round((key.point.Y - screen.Top) * 65535.0 / Math.Max(1, screen.Height - 1));
                    uint data = message == 0x20A || message == 0x20E ? unchecked((uint)(int)(short)(key.data >> 16)) : message == 0x20B || message == 0x20C ? key.data >> 16 : 0;
                    queued.Enqueue(new Input { type = 0, union = new InputUnion { mouse = new MouseInput { dx = x, dy = y, data = data, flags = flags | 0xC001u, extra = new UIntPtr(ReplayedInput) } } });
                    return true;
                }
            }

            public Input[] Take(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || window != owner)
                        return null;
                    active = false;
                    owner = IntPtr.Zero;
                    Input[] result = new Input[queued.Count + (deferred.HasValue ? 1 : 0)];
                    int offset = 0;
                    if (deferred.HasValue)
                        result[offset++] = deferred.Value;
                    queued.CopyTo(result, offset);
                    queued.Clear();
                    heldModifiers.Clear();
                    initialReleases.Clear();
                    deferred = inFlight = null;
                    return result;
                }
            }

            // A focus change can race SendInput. Reclaim the one in-flight event
            // before the hook forwards it, preserving its place ahead of later keys.
            public bool DeferInFlight(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || window != owner || !inFlight.HasValue || deferred.HasValue)
                        return false;
                    deferred = inFlight;
                    inFlight = null;
                    return true;
                }
            }

            // A drained key can itself start the next conversion. Acknowledge its
            // delivery before that conversion tries to finish an otherwise empty
            // queue; scheduling the next drain alone leaves a stale in-flight key.
            public bool AcknowledgeInFlight(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || window != owner || !inFlight.HasValue)
                        return false;
                    inFlight = null;
                    return true;
                }
            }

            // Keep capturing fresh input until every older event has been delivered.
            // A replayed delimiter may start another correction before the next event.
            public Input[] DrainNext(IntPtr window)
            {
                lock (gate)
                {
                    if (!active || window != owner)
                        return null;
                    if (queued.Count == 0 && !deferred.HasValue)
                    {
                        active = false;
                        owner = IntPtr.Zero;
                        heldModifiers.Clear();
                        initialReleases.Clear();
                        inFlight = null;
                        return new Input[0];
                    }

                    Input next;
                    if (deferred.HasValue)
                    {
                        next = deferred.Value;
                        deferred = null;
                    }
                    else
                        next = queued.Dequeue();
                    inFlight = next;
                    if (next.type == 1)
                        next.union.keyboard.extra = new UIntPtr(DrainedInput);
                    else
                        next.union.mouse.extra = new UIntPtr(DrainedInput);
                    return new[]
                    {
                        next
                    };
                }
            }
        }

        public static bool ReplayCorrectionInput(Input[] keys)
        {
            if (keys == null)
                return false;
            if (keys.Length == 0)
                return true;
            return SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }

        public static Input[] UnicodeText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > 160)
                return null;
            var result = new Input[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsSurrogate(text[i]) || char.IsControl(text[i]))
                    return null;
                Input[] character = UnicodeCharacter(text[i].ToString());
                result[i * 2] = character[0];
                result[i * 2 + 1] = character[1];
            }

            return result;
        }

        // One SendInput batch: no physical keystroke can land between selection
        // and replacement. Keep the existing delimiter and any proven typed suffix.
        public static Input[] AutomaticCorrectionInput(string input, string output, string suffix)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output) || input.Length < 2 || output.Length < 2 || input.Length > 160 || output.Length > 160 || suffix == null || suffix.Length > 96 || input[input.Length - 1] != output[output.Length - 1] || !Core.CompletionDelimiter(input[input.Length - 1]) && input[input.Length - 1] != '\u00a0')
                return null;
            string before = input.Substring(0, input.Length - 1), after = output.Substring(0, output.Length - 1);
            foreach (char c in before + suffix)
                if (char.IsSurrogate(c) || char.IsControl(c) && c != '\n' && c != '\t')
                    return null;
            Input[] text = UnicodeText(after);
            if (text == null)
                return null;
            // Change only the differing characters. Common contractions insert one
            // apostrophe; their existing letters are never selected or retyped.
            int prefix = 0;
            while (prefix < before.Length && prefix < after.Length && before[prefix] == after[prefix])
                prefix++;
            int common = 0;
            while (common < before.Length - prefix && common < after.Length - prefix && before[before.Length - 1 - common] == after[after.Length - 1 - common])
                common++;
            int removed = before.Length - prefix - common, added = after.Length - prefix - common, distance = suffix.Length + 1 + common;
            var keys = new System.Collections.Generic.List<Input>();
            for (int n = 0; n < distance; n++)
            {
                keys.Add(Key(0x25, false));
                keys.Add(Key(0x25, true));
            }

            if (removed > 0)
            {
                keys.Add(Key(0x10, false));
                for (int n = 0; n < removed; n++)
                {
                    keys.Add(Key(0x25, false));
                    keys.Add(Key(0x25, true));
                }

                keys.Add(Key(0x10, true));
            }

            if (added > 0)
            {
                var replacement = UnicodeText(after.Substring(prefix, added));
                if (replacement == null)
                    return null;
                keys.AddRange(replacement);
            }
            else if (removed > 0)
            {
                keys.Add(Key(8, false));
                keys.Add(Key(8, true));
            }

            for (int n = 0; n < distance; n++)
            {
                keys.Add(Key(0x27, false));
                keys.Add(Key(0x27, true));
            }

            for (int n = 0; n < keys.Count; n++)
            {
                Input item = keys[n];
                item.union.keyboard.extra = new UIntPtr(OwnCorrectionInput);
                keys[n] = item;
            }

            return keys.ToArray();
        }

        public static bool CorrectAutomatic(string input, string output, string suffix)
        {
            foreach (int modifier in new[]
            {
                0x10,
                0x11,
                0x12,
                0x5B,
                0x5C
            }

            )
                if ((GetAsyncKeyState(modifier) & 0x8000) != 0)
                    return false;
            Input[] keys = AutomaticCorrectionInput(input, output, suffix);
            if (keys == null)
                return false;
            uint sent = SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input)));
            if (sent != keys.Length)
            {
                Input[] release =
                {
                    Key(0x10, true)
                };
                SendInput(1, release, Marshal.SizeOf(typeof(Input)));
                return false;
            }

            return true;
        }

        public static bool TypeText(string text)
        {
            Input[] input = UnicodeText(text);
            return input != null && SendInput((uint)input.Length, input, Marshal.SizeOf(typeof(Input))) == input.Length;
        }

        public static bool TextNeedsClipboard(string text, bool formatted)
        {
            return formatted || UnicodeText(text) == null;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point p);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int key);
        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] state);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicodeEx(uint vk, uint scan, byte[] state, StringBuilder text, int max, uint flags, IntPtr layout);
        [DllImport("user32.dll")]
        public static extern IntPtr GetKeyboardLayout(uint thread);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint process);
        [StructLayout(LayoutKind.Sequential)]
        struct FocusRect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct GuiThreadInfo
        {
            public int Size, Flags;
            public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
            public FocusRect CaretRect;
        }

        [DllImport("user32.dll")]
        static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
        // This may be called inside a keyboard hook: never call an accessibility
        // provider or send a synchronous message to the target application's thread.
        // Exact editable-element/text ownership is checked separately by SafeEdit.
        public static int[] FocusIdentity()
        {
            IntPtr owner = GetForegroundWindow();
            if (owner == IntPtr.Zero)
                return null;
            uint pid;
            uint thread = GetWindowThreadProcessId(owner, out pid);
            var info = new GuiThreadInfo
            {
                Size = Marshal.SizeOf(typeof(GuiThreadInfo))
            };
            if (thread == 0 || !GetGUIThreadInfo(thread, ref info))
                return null;
            long window = owner.ToInt64(), focus = info.Focus.ToInt64();
            return new[]
            {
                unchecked((int)window),
                unchecked((int)(window >> 32)),
                unchecked((int)pid),
                unchecked((int)thread),
                unchecked((int)focus),
                unchecked((int)(focus >> 32))
            };
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint access, bool inherit, uint process);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref uint size);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        public static bool ProcessImageIs(uint pid, string name)
        {
            IntPtr handle = OpenProcess(0x1000, false, pid);
            if (handle == IntPtr.Zero)
                return false;
            try
            {
                uint size = 1024;
                var path = new StringBuilder((int)size);
                return QueryFullProcessImageName(handle, 0, path, ref size) && System.IO.Path.GetFileName(path.ToString()).Equals(name, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hwnd, int id);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint count, Input[] input, int size);
        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEvent callback, uint process, uint thread, uint flags);
        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr handle);
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string text);
        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr menu);
        [DllImport("user32.dll")]
        public static extern uint TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr reserved);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr EditMessageTimeout(IntPtr hwnd, uint msg, IntPtr w, IntPtr l, uint flags, uint milliseconds, out UIntPtr result);
        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr EditTextTimeout(IntPtr hwnd, uint msg, IntPtr w, StringBuilder l, uint flags, uint milliseconds, out UIntPtr result);
        public static bool TryReadEditText(IntPtr handle, out string text)
        {
            text = null;
            if (handle == IntPtr.Zero || !IsWindow(handle))
                return false;
            var name = new StringBuilder(128);
            GetClassName(handle, name, name.Capacity);
            string kind = name.ToString();
            if (!kind.Equals("Edit", StringComparison.OrdinalIgnoreCase) && !kind.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) && !kind.StartsWith("WindowsForms10.EDIT.", StringComparison.OrdinalIgnoreCase))
                return false;
            UIntPtr result;
            if (EditMessageTimeout(handle, 0xE, IntPtr.Zero, IntPtr.Zero, 0x23, 75, out result) == IntPtr.Zero)
                return false;
            ulong length = result.ToUInt64();
            if (length > 200000)
                return false;
            var buffer = new StringBuilder((int)length + 1);
            if (EditTextTimeout(handle, 0xD, new IntPtr(buffer.Capacity), buffer, 0x23, 75, out result) == IntPtr.Zero || result.ToUInt64() > length)
                return false;
            // Recheck length: a concurrent edit or truncated read cannot authorize replacement.
            UIntPtr after;
            if (EditMessageTimeout(handle, 0xE, IntPtr.Zero, IntPtr.Zero, 0x23, 75, out after) == IntPtr.Zero || after.ToUInt64() != length || buffer.Length != (int)result.ToUInt64())
                return false;
            text = kind.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) ? buffer.ToString().Replace("\r\n", "\r") : buffer.ToString();
            return true;
        }

        public static string EditTextCoordinates(IntPtr handle, string text)
        {
            if (text == null)
                return null;
            var kind = new StringBuilder(128);
            GetClassName(handle, kind, kind.Capacity);
            return kind.ToString().StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) ? text.Replace("\r\n", "\r").Replace('\n', '\r') : text;
        }

        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
        static extern IntPtr EditSelectionTimeout(IntPtr hwnd, uint msg, out int start, out int end, uint flags, uint milliseconds, out UIntPtr result);
        public static void GetEditSelection(IntPtr handle, out int start, out int end)
        {
            UIntPtr result;
            if (EditSelectionTimeout(handle, 0xB0, out start, out end, 0x23, 75, out result) == IntPtr.Zero)
            {
                start = end = -1;
                throw new InvalidOperationException("Edit selection unavailable");
            }
        }

        public static bool SelectEditRange(IntPtr handle, int start, int end)
        {
            UIntPtr result;
            return EditMessageTimeout(handle, 0xB1, new IntPtr(start), new IntPtr(end), 0x23, 75, out result) != IntPtr.Zero;
        }

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll", EntryPoint = "SendMessageW")]
        public static extern IntPtr EditSelection(IntPtr hwnd, uint msg, out int start, out int end);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        [DllImport("shcore.dll")]
        public static extern int SetProcessDpiAwareness(int awareness);
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int x, int y, uint flags);
        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr icon);
        public static void DpiAware()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                    return;
            }
            catch (EntryPointNotFoundException)
            {
            }

            try
            {
                SetProcessDpiAwareness(2);
            }
            catch (DllNotFoundException)
            {
                SetProcessDPIAware();
            }
        }

        public static int CursorDpi()
        {
            Point p;
            GetCursorPos(out p);
            try
            {
                uint x, y;
                if (GetDpiForMonitor(MonitorFromPoint(p, 2), 0, out x, out y) == 0)
                    return (int)x;
            }
            catch (DllNotFoundException)
            {
            }

            return 96;
        }

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint key, uint mapType);
        // Navigation keys need an explicit extended flag even if MapVirtualKey omits E0.
        // Otherwise Num Lock keypad emulation can release Shift around each arrow.
        static Input Key(ushort key, bool up)
        {
            if (key == 0x10)
                key = 0xA0;
            else if (key == 0x11)
                key = 0xA2;
            else if (key == 0x12)
                key = 0xA4;
            uint scan = MapVirtualKey(key, 4);
            uint flags = (up ? 2u : 0u) | (((scan & 0xff00) != 0 || (key >= 0x21 && key <= 0x28) || key == 0x2D || key == 0x2E) ? 1u : 0u);
            return new Input
            {
                type = 1,
                union = new InputUnion
                {
                    keyboard = new KeyboardInput
                    {
                        vk = key,
                        scan = (ushort)(scan & 0xff),
                        flags = flags,
                        extra = new UIntPtr(OwnInput)
                    }
                }
            };
        }

        public static Input[] UnicodeCharacter(string value)
        {
            if (value == null || value.Length != 1 || char.IsSurrogate(value[0]))
                throw new ArgumentException("Expected one BMP character");
            return new[]
            {
                new Input
                {
                    type = 1,
                    union = new InputUnion
                    {
                        keyboard = new KeyboardInput
                        {
                            vk = 0,
                            scan = value[0],
                            flags = 4,
                            extra = new UIntPtr(OwnInput)
                        }
                    }
                },
                new Input
                {
                    type = 1,
                    union = new InputUnion
                    {
                        keyboard = new KeyboardInput
                        {
                            vk = 0,
                            scan = value[0],
                            flags = 6,
                            extra = new UIntPtr(OwnInput)
                        }
                    }
                }
            };
        }

        public static bool TypeCharacter(string value)
        {
            Input[] keys = UnicodeCharacter(value);
            return SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }

        public static bool Paste()
        {
            Input[] keys =
            {
                Key(0x11, false),
                Key(0x56, false),
                Key(0x56, true),
                Key(0x11, true)
            };
            return SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }

        public static bool Chord(ushort key, bool control = false, bool shift = false)
        {
            var keys = new System.Collections.Generic.List<Input>();
            if (control)
                keys.Add(Key(0x11, false));
            if (shift)
                keys.Add(Key(0x10, false));
            keys.Add(Key(key, false));
            keys.Add(Key(key, true));
            if (shift)
                keys.Add(Key(0x10, true));
            if (control)
                keys.Add(Key(0x11, true));
            return SendInput((uint)keys.Count, keys.ToArray(), Marshal.SizeOf(typeof(Input))) == keys.Count;
        }

        public static bool SelectFieldRange(int start, int end)
        {
            if (start < 0 || end < start || end > 4096)
                return false;
            var keys = new System.Collections.Generic.List<Input>();
            keys.Add(Key(0x11, false));
            keys.Add(Key(0x24, false));
            keys.Add(Key(0x24, true));
            keys.Add(Key(0x11, true));
            for (int n = 0; n < start; n++)
            {
                keys.Add(Key(0x27, false));
                keys.Add(Key(0x27, true));
            }

            if (end > start)
            {
                keys.Add(Key(0x10, false));
                for (int n = start; n < end; n++)
                {
                    keys.Add(Key(0x27, false));
                    keys.Add(Key(0x27, true));
                }

                keys.Add(Key(0x10, true));
            }

            return SendInput((uint)keys.Count, keys.ToArray(), Marshal.SizeOf(typeof(Input))) == keys.Count;
        }

        // A known single-line buffer permits navigation from its nearest end.
        // The caller must still prove the resulting selection before replacement.
        public static bool SelectFieldRange(int start, int end, int length, bool control = true)
        {
            Input[] keys = FieldRangeKeys(start, end, length, control);
            return keys != null && SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }

        public static Input[] FieldRangeKeys(int start, int end, int length, bool control = true)
        {
            if (start < 0 || end < start || length < end || length > 4096)
                return null;
            var keys = new System.Collections.Generic.List<Input>();
            Action<ushort, bool, bool> chord = (key, useControl, shift) =>
            {
                if (useControl)
                    keys.Add(Key(0x11, false));
                if (shift)
                    keys.Add(Key(0x10, false));
                keys.Add(Key(key, false));
                keys.Add(Key(key, true));
                if (shift)
                    keys.Add(Key(0x10, true));
                if (useControl)
                    keys.Add(Key(0x11, true));
            };
            if (start == 0 && end == length && length > 2)
            {
                chord(0x24, control, false);
                chord(0x23, control, true);
                return keys.ToArray();
            }

            bool fromEnd = length - end < start;
            chord(fromEnd ? (ushort)0x23 : (ushort)0x24, control, false);
            int distance = fromEnd ? length - end : start;
            ushort direction = fromEnd ? (ushort)0x25 : (ushort)0x27;
            for (int n = 0; n < distance; n++)
            {
                keys.Add(Key(direction, false));
                keys.Add(Key(direction, true));
            }

            if (end > start)
            {
                keys.Add(Key(0x10, false));
                for (int n = start; n < end; n++)
                {
                    keys.Add(Key(direction, false));
                    keys.Add(Key(direction, true));
                }

                keys.Add(Key(0x10, true));
            }

            return keys.ToArray();
        }

        public static bool SelectFollowing(int count)
        {
            if (count < 1 || count > 160)
                return false;
            var keys = new System.Collections.Generic.List<Input>();
            keys.Add(Key(0x10, false));
            for (int n = 0; n < count; n++)
            {
                keys.Add(Key(0x27, false));
                keys.Add(Key(0x27, true));
            }

            keys.Add(Key(0x10, true));
            return SendInput((uint)keys.Count, keys.ToArray(), Marshal.SizeOf(typeof(Input))) == keys.Count;
        }

        public static Input[] CaretNavigation(int delta)
        {
            if (delta == 0 || delta < -192 || delta > 192)
                return null;
            int count = Math.Abs(delta);
            ushort key = delta < 0 ? (ushort)0x25 : (ushort)0x27;
            var inputs = new Input[count * 2];
            for (int i = 0; i < count; i++)
            {
                inputs[i * 2] = Key(key, false);
                inputs[i * 2 + 1] = Key(key, true);
            }

            return inputs;
        }

        public static bool MoveCaretCharacters(int delta)
        {
            if (delta == 0)
                return true;
            if ((GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0 || (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0)
                return false;
            var keys = CaretNavigation(delta);
            return keys != null && SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }

        public static bool SelectPrevious(int count)
        {
            if (count < 1 || count > 160 || (GetAsyncKeyState(0x11) & 0x8000) != 0 || (GetAsyncKeyState(0x10) & 0x8000) != 0 || (GetAsyncKeyState(0x12) & 0x8000) != 0)
                return false;
            Input[] keys = new Input[count * 2 + 2];
            keys[0] = Key(0x10, false);
            for (int i = 0; i < count; i++)
            {
                keys[1 + i * 2] = Key(0x25, false);
                keys[2 + i * 2] = Key(0x25, true);
            }

            keys[keys.Length - 1] = Key(0x10, true);
            uint sent = SendInput((uint)keys.Length, keys, Marshal.SizeOf(typeof(Input)));
            if (sent != keys.Length)
            {
                Input[] release =
                {
                    Key(0x10, true)
                };
                SendInput(1, release, Marshal.SizeOf(typeof(Input)));
                return false;
            }

            return true;
        }

        public static void Backspace()
        {
            Input[] keys =
            {
                Key(8, false),
                Key(8, true)
            };
            SendInput(2, keys, Marshal.SizeOf(typeof(Input)));
        }

        public static bool Escape()
        {
            Input[] keys =
            {
                Key(27, false),
                Key(27, true)
            };
            return SendInput(2, keys, Marshal.SizeOf(typeof(Input))) == keys.Length;
        }
    }
}
