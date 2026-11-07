// Foreign OLE data can fault inside the CLR before a managed catch can run.
// Materialize it in a disposable STA child. The resident accepts only bounded,
// explicitly typed values, never serialized CLR objects or COM interfaces.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyJaya
{
    internal sealed class ClipboardCaptureFailures
    {
        readonly object gate = new object ();
        uint sequence;
        bool failed;
        internal bool Allows(uint current)
        {
            lock (gate)
                return !failed || sequence != current;
        }

        internal void Record(uint current)
        {
            lock (gate)
            {
                sequence = current;
                failed = true;
            }
        }
    }

    internal sealed class ClipboardSnapshot
    {
        internal const string Argument = "--capture-clipboard";
        const int Limit = 64 * 1024 * 1024;
        internal readonly DataObject Data;
        internal readonly uint Sequence;
        internal string Text
        {
            get
            {
                return Data.GetData(DataFormats.UnicodeText, false) as string ?? "";
            }
        }

        [ThreadStatic]
        internal static string LastFailure;
        [ThreadStatic]
        static bool failedProvider;
        static readonly ClipboardCaptureFailures failures = new ClipboardCaptureFailures();
        static int capturing;
        internal static Action<string> Trace;
        internal ClipboardSnapshot(DataObject data, uint sequence)
        {
            Data = data;
            Sequence = sequence;
        }

        internal delegate bool UnicodeReader(uint sequence, int limit, out string text);
        internal static string ReadText(Func<uint> sequence, UnicodeReader read, Func<string> failure)
        {
            uint before = sequence();
            string text;
            bool readable = read(before, 8 * 1024 * 1024, out text);
            if (before == sequence())
            {
                if (readable)
                    return text;
                if (failure() == "Unicode payload unavailable")
                    return null;
            }

            // Busy, stale or malformed data must not become a successful empty
            // clipboard observation that can authorize later cycling.
            throw new ExternalException("Clipboard text unavailable");
        }

        internal static ClipboardSnapshot Capture()
        {
            if (Interlocked.CompareExchange(ref capturing, 1, 0) != 0)
            {
                LastFailure = "clipboard capture already active";
                return null;
            }

            try
            {
                var snapshot = Capture(() => Process.Start(new ProcessStartInfo(Application.ExecutablePath, Argument) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true }), Native.GetClipboardSequenceNumber, 1500, failures);
                if (snapshot == null)
                    return null;
                string raw;
                bool readable = Native.ClipboardUnicode(snapshot.Sequence, 8 * 1024 * 1024, out raw);
                bool unicode = snapshot.Data.GetDataPresent(DataFormats.UnicodeText, false);
                if ((readable ? !unicode || snapshot.Text != raw : unicode || Native.ClipboardReadFailure != "Unicode payload unavailable") || snapshot.Sequence != Native.GetClipboardSequenceNumber())
                {
                    LastFailure = "native Unicode snapshot verification failed";
                    return null;
                }

                return snapshot;
            }
            finally
            {
                Interlocked.Exchange(ref capturing, 0);
            }
        }

        // Injected children/sequence readers let tests exercise process death,
        // stalls, malformed output and clipboard replacement without OLE access.
        internal static ClipboardSnapshot Capture(Func<Process> start, Func<uint> sequence, int timeout, ClipboardCaptureFailures failures = null)
        {
            LastFailure = null;
            failedProvider = false;
            Process child = null;
            Task<ClipboardSnapshot> read = null;
            uint before = sequence();
            if (failures != null && !failures.Allows(before))
            {
                LastFailure = "clipboard provider failed for unchanged sequence";
                return null;
            }

            try
            {
                child = start();
                if (child == null)
                    return null;
                var input = child.StandardOutput.BaseStream;
                read = Task.Factory.StartNew(() => Decode(input));
                var clock = Stopwatch.StartNew();
                while (!read.IsCompleted && clock.ElapsedMilliseconds < timeout)
                {
                    // A restored clipboard can belong to this resident. Its STA
                    // must service the child's OLE delayed-render requests.
                    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                        Application.DoEvents();
                    Thread.Sleep(5);
                }

                if (!read.IsCompleted)
                {
                    failedProvider = true;
                    LastFailure = "clipboard helper timed out";
                    return null;
                }

                var value = read.Result;
                bool exited = child.WaitForExit(150);
                if (!exited || child.ExitCode != 0)
                {
                    // A provider can fault or hang after closing a complete
                    // output frame. Treat that as the same failed sequence as
                    // an earlier crash; exit 1 remains a normal refusal.
                    failedProvider = !exited || !RefusalExit(child.ExitCode);
                    LastFailure = exited && RefusalExit(child.ExitCode) ? RefusalReason(child.ExitCode) : "clipboard helper did not exit successfully";
                    return null;
                }

                if (value == null || value.Sequence != before || sequence() != before)
                {
                    LastFailure = "clipboard helper unavailable or sequence changed";
                    return null;
                }

                return value;
            }
            catch (Exception error)
            {
                try
                {
                    failedProvider = child != null && (!child.WaitForExit(100) || !RefusalExit(child.ExitCode));
                }
                catch (Exception)
                {
                    failedProvider = true;
                }

                LastFailure = "clipboard helper " + error.GetBaseException().GetType().Name;
                try
                {
                    if (child != null && child.HasExited)
                        LastFailure = RefusalExit(child.ExitCode) || child.ExitCode == 2 ? RefusalReason(child.ExitCode) : LastFailure + " exit=" + child.ExitCode;
                }
                catch (Exception)
                {
                }

                return null;
            }
            finally
            {
                if (failures != null && failedProvider && before == sequence())
                {
                    failures.Record(before);
                    try
                    {
                        if (Trace != null)
                            Trace("Clipboard capture helper unavailable; clipboard unchanged: " + LastFailure);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (child != null)
                {
                    try
                    {
                        child.StandardInput.Close();
                        if (!child.WaitForExit(100))
                        {
                            child.Kill();
                            child.WaitForExit(250);
                        }
                    }
                    catch (Exception)
                    {
                    }

                    child.Dispose();
                }

                if (read != null)
                    read.ContinueWith(task =>
                    {
                        var ignored = task.Exception;
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        internal sealed class Refused : Exception
        {
            internal readonly int Code;
            internal Refused(int code) : base(RefusalReason(code))
            {
                Code = code;
            }
        }

        static bool RefusalExit(int code)
        {
            return code == 1 || code >= 3 && code <= 8;
        }

        static string RefusalReason(int code)
        {
            switch (code)
            {
                case 1:
                    return "clipboard helper declined data";
                case 2:
                    return "clipboard helper lost its parent pipe";
                case 3:
                    return "clipboard helper could not see the nonempty clipboard";
                case 4:
                    return "clipboard helper format limit";
                case 5:
                    return "clipboard changed during capture";
                case 6:
                    return "clipboard provider read failed";
                case 7:
                    return "clipboard format unavailable or unsupported";
                default:
                    return "clipboard snapshot encoding failed";
            }
        }

        internal static DataObject ReadSource(Func<IDataObject> getSource, Func<uint> sequence, uint expected, Func<bool> empty, Func<string, bool> emptyAdvertisement, Action<int> wait)
        {
            IDataObject source = null;
            string[] formats = null;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (sequence() != expected)
                    throw new Refused(5);
                try
                {
                    source = getSource();
                    formats = source == null ? new string[0] : source.GetFormats(false);
                }
                catch (Exception)
                {
                    throw new Refused(6);
                }

                if (sequence() != expected)
                    throw new Refused(5);
                if (formats == null)
                    throw new Refused(6);
                if (formats.Length > 128)
                    throw new Refused(4);
                if (formats.Length != 0 || empty())
                    break;
                if (attempt == 2)
                    throw new Refused(3);
                wait(20);
            }

            var data = new DataObject();
            foreach (string format in formats)
            {
                object value;
                try
                {
                    value = source.GetData(format, false);
                }
                catch (Exception)
                {
                    throw new Refused(6);
                }

                if (value == null && emptyAdvertisement(format))
                    continue;
                try
                {
                    data.SetData(format, false, SelectionConversion.CloneClipboardValue(value));
                }
                catch (Exception)
                {
                    throw new Refused(7);
                }
            }

            if (sequence() != expected)
                throw new Refused(5);
            return data;
        }

        internal static bool RestoreData(DataObject data, Func<bool> clear, Action<DataObject> set)
        {
            // Publishing an empty OLE object is different from restoring an
            // actually empty Win32 clipboard. Keep the original absence of data
            // through the same atomic temporary-payload ownership check.
            if (data.GetFormats(false).Length == 0)
                return clear();
            set(data);
            return true;
        }

        static void String(BinaryWriter writer, string text)
        {
            if (text == null || text.Length > Limit / 2)
                throw new InvalidDataException("Clipboard string too large");
            writer.Write(text.Length);
            // Preserve even unpaired UTF-16 units; no encoder fallback may
            // change a user's clipboard while making the snapshot portable.
            foreach (char c in text)
                writer.Write((ushort)c);
        }

        static string String(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > maximum)
                throw new InvalidDataException("Invalid clipboard string length");
            var chars = new char[count];
            for (int i = 0; i < count; i++)
                chars[i] = (char)reader.ReadUInt16();
            return new string (chars);
        }

        static void Bytes(BinaryWriter writer, byte[] bytes)
        {
            if (bytes.Length > Limit / 2)
                throw new InvalidDataException("Clipboard value too large");
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        static byte[] Bytes(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > Limit / 2)
                throw new InvalidDataException("Invalid clipboard value length");
            byte[] bytes = reader.ReadBytes(count);
            if (bytes.Length != count)
                throw new EndOfStreamException();
            return bytes;
        }

        internal static void Encode(Stream output, ClipboardSnapshot snapshot)
        {
            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer))
            {
                writer.Write(0x4B4A4353);
                writer.Write(1);
                writer.Write(snapshot.Sequence);
                string[] formats = snapshot.Data.GetFormats(false);
                if (formats.Length > 128)
                    throw new InvalidDataException("Too many clipboard formats");
                writer.Write(formats.Length);
                foreach (string format in formats)
                {
                    if (format.Length == 0 || format.Length > 256)
                        throw new InvalidDataException("Invalid clipboard format");
                    String(writer, format);
                    object value = snapshot.Data.GetData(format, false);
                    if (value is string)
                    {
                        writer.Write((byte)1);
                        String(writer, (string)value);
                    }
                    else if (value is byte[])
                    {
                        writer.Write((byte)2);
                        Bytes(writer, (byte[])value);
                    }
                    else if (value is MemoryStream)
                    {
                        writer.Write((byte)3);
                        Bytes(writer, ((MemoryStream)value).ToArray());
                    }
                    else if (value is string[])
                    {
                        var items = (string[])value;
                        if (items.Length > 4096)
                            throw new InvalidDataException("Too many clipboard strings");
                        writer.Write((byte)4);
                        writer.Write(items.Length);
                        foreach (string item in items)
                            String(writer, item);
                    }
                    else if (value is Bitmap)
                    {
                        var bitmap = (Bitmap)value;
                        if (bitmap.Width <= 0 || bitmap.Height <= 0 || (long)bitmap.Width * bitmap.Height > Limit / 8)
                            throw new InvalidDataException("Clipboard bitmap too large");
                        writer.Write((byte)5);
                        writer.Write(bitmap.Width);
                        writer.Write(bitmap.Height);
                        writer.Write(bitmap.HorizontalResolution);
                        writer.Write(bitmap.VerticalResolution);
                        using (var image = new MemoryStream())
                        {
                            bitmap.Save(image, ImageFormat.Png);
                            Bytes(writer, image.ToArray());
                        }
                    }
                    else if (value is Metafile)
                    {
                        writer.Write((byte)6);
                        IntPtr handle = ((Metafile)value).GetHenhmetafile();
                        try
                        {
                            uint count = GetEnhMetaFileBits(handle, 0, null);
                            if (count == 0 || count > Limit / 2)
                                throw new InvalidDataException("Metafile unavailable");
                            byte[] bytes = new byte[(int)count];
                            if (GetEnhMetaFileBits(handle, count, bytes) != count)
                                throw new InvalidDataException("Metafile incomplete");
                            Bytes(writer, bytes);
                        }
                        finally
                        {
                            DeleteEnhMetaFile(handle);
                        }
                    }
                    else
                        throw new InvalidDataException("Unsupported clipboard value");
                    if (buffer.Length > Limit)
                        throw new InvalidDataException("Clipboard snapshot too large");
                }

                writer.Flush();
                output.Write(buffer.GetBuffer(), 0, checked((int)buffer.Length));
            }
        }

        internal static ClipboardSnapshot Decode(Stream input)
        {
            using (var limited = new LimitedInput(input, Limit))
            using (var reader = new BinaryReader(limited))
            {
                if (reader.ReadInt32() != 0x4B4A4353 || reader.ReadInt32() != 1)
                    throw new InvalidDataException("Invalid clipboard protocol");
                uint sequence = reader.ReadUInt32();
                int count = reader.ReadInt32();
                if (count < 0 || count > 128)
                    throw new InvalidDataException("Invalid clipboard format count");
                var data = new DataObject();
                long expandedImages = 0;
                for (int i = 0; i < count; i++)
                {
                    string format = String(reader, 256);
                    if (format.Length == 0 || data.GetDataPresent(format, false))
                        throw new InvalidDataException("Invalid or duplicate clipboard format");
                    object value;
                    switch (reader.ReadByte())
                    {
                        case 1:
                            value = String(reader, Limit / 2);
                            break;
                        case 2:
                            value = Bytes(reader);
                            break;
                        case 3:
                            value = new MemoryStream(Bytes(reader), false);
                            break;
                        case 4:
                            int n = reader.ReadInt32();
                            if (n < 0 || n > 4096)
                                throw new InvalidDataException("Invalid clipboard string count");
                            var items = new string[n];
                            for (int j = 0; j < n; j++)
                                items[j] = String(reader, Limit / 2);
                            value = items;
                            break;
                        case 5:
                            int width = reader.ReadInt32(), height = reader.ReadInt32();
                            float xdpi = reader.ReadSingle(), ydpi = reader.ReadSingle();
                            byte[] png = Bytes(reader);
                            expandedImages += (long)width * height * 4;
                            if (expandedImages > Limit / 2)
                                throw new InvalidDataException("Clipboard bitmap expansion exceeds limit");
                            if (width <= 0 || height <= 0 || (long)width * height > Limit / 8 || xdpi <= 0 || ydpi <= 0 || float.IsInfinity(xdpi) || float.IsInfinity(ydpi) || float.IsNaN(xdpi) || float.IsNaN(ydpi) || !PngSize(png, width, height))
                                throw new InvalidDataException("Invalid clipboard bitmap dimensions");
                            using (var image = new MemoryStream(png))
                            using (var bitmap = new Bitmap(image))
                            {
                                var copy = (Bitmap)bitmap.Clone();
                                copy.SetResolution(xdpi, ydpi);
                                value = copy;
                            }

                            break;
                        case 6:
                            byte[] bytes = Bytes(reader);
                            IntPtr handle = SetEnhMetaFileBits((uint)bytes.Length, bytes);
                            if (handle == IntPtr.Zero)
                                throw new InvalidDataException("Invalid clipboard metafile");
                            try
                            {
                                value = new Metafile(handle, true);
                                handle = IntPtr.Zero;
                            }
                            finally
                            {
                                if (handle != IntPtr.Zero)
                                    DeleteEnhMetaFile(handle);
                            }

                            break;
                        default:
                            throw new InvalidDataException("Unsupported clipboard type");
                    }

                    data.SetData(format, false, value);
                }

                if (limited.ReadByte() != -1)
                    throw new InvalidDataException("Trailing clipboard data");
                return new ClipboardSnapshot(data, sequence);
            }
        }

        static bool PngSize(byte[] bytes, int width, int height)
        {
            byte[] signature =
            {
                137,
                80,
                78,
                71,
                13,
                10,
                26,
                10
            };
            if (bytes.Length < 24)
                return false;
            if (bytes[8] != 0 || bytes[9] != 0 || bytes[10] != 0 || bytes[11] != 13 || bytes[12] != 73 || bytes[13] != 72 || bytes[14] != 68 || bytes[15] != 82)
                return false;
            for (int i = 0; i < signature.Length; i++)
                if (bytes[i] != signature[i])
                    return false;
            uint w = 0, h = 0;
            for (int i = 0; i < 4; i++)
            {
                w = (w << 8) | bytes[16 + i];
                h = (h << 8) | bytes[20 + i];
            }

            return w == width && h == height;
        }

        sealed class LimitedInput : Stream
        {
            readonly Stream input;
            int remaining;
            internal LimitedInput(Stream input, int maximum)
            {
                this.input = input;
                remaining = maximum;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (count > remaining)
                    throw new InvalidDataException("Clipboard snapshot exceeds limit");
                int got = input.Read(buffer, offset, count);
                remaining -= got;
                return got;
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long length)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override void Flush()
            {
            }
        }

        internal static int HelperMain()
        {
            SetErrorMode(3);
            // No resident mutex/profile/hooks, and no clipboard writes. EOF
            // means the parent has gone away, including during a provider hang.
            new Thread(() =>
            {
                try
                {
                    using (var pipe = Console.OpenStandardInput())
                        Program.WaitForParent(pipe);
                }
                catch (IOException)
                {
                }

                Environment.Exit(2);
            })
            {
                IsBackground = true
            }.Start();
            try
            {
                uint sequence = Native.GetClipboardSequenceNumber();
                var data = ReadSource(Clipboard.GetDataObject, Native.GetClipboardSequenceNumber, sequence, () => Native.ClipboardActuallyEmpty(sequence), Native.EmptyAdobeClipboardAdvertisement, Thread.Sleep);
                using (var output = Console.OpenStandardOutput())
                    Encode(output, new ClipboardSnapshot(data, sequence));
                return 0;
            }
            catch (Refused refusal)
            {
                return refusal.Code;
            }
            catch (Exception)
            {
                return 8;
            }
        }

        [DllImport("kernel32.dll")]
        static extern uint SetErrorMode(uint mode);
        [DllImport("gdi32.dll")]
        static extern uint GetEnhMetaFileBits(IntPtr handle, uint size, [Out] byte[] bytes);
        [DllImport("gdi32.dll")]
        static extern IntPtr SetEnhMetaFileBits(uint size, byte[] bytes);
        [DllImport("gdi32.dll")]
        static extern bool DeleteEnhMetaFile(IntPtr handle);
    }
}
