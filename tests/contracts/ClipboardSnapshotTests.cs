// Real disposable children and in-memory data objects; no system clipboard/OLE.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace KeyJaya
{
    internal static class ClipboardSnapshotTests
    {
        static int checks;
        static void Check(bool value, string message)
        {
            if (!value)
                throw new Exception("Clipboard snapshot: " + message);
            checks++;
        }

        static DataObject Sample()
        {
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, false, "Test \ud800 text कṛṣṇa\r\n");
            data.SetData("snapshot-bytes", false, new byte[] { 1, 0, 255 });
            data.SetData("HTML Format", false, new MemoryStream(new byte[] { 9, 8, 7 }));
            data.SetData(DataFormats.FileDrop, false, new[] { "first", "second" });
            return data;
        }

        internal static int Fixture(string mode)
        {
            if (mode == "exit")
                return 23;
            if (mode == "unsupported")
                return 1;
            if (mode == "empty-view")
                return 3;
            if (mode == "stall")
            {
                Thread.Sleep(15000);
                return 0;
            }

            using (var output = Console.OpenStandardOutput())
            {
                if (mode == "bad")
                {
                    output.WriteByte(0);
                    return 0;
                }

                ClipboardSnapshot.Encode(output, new ClipboardSnapshot(Sample(), mode == "wrong" ? 42u : 41u));
            }

            if (mode == "complete-stall")
                Thread.Sleep(15000);
            return mode == "complete-exit" ? 23 : 0;
        }

        static Process Start(string mode)
        {
            return Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--clipboard-fixture " + mode) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true });
        }

        static bool Alive(int id)
        {
            try
            {
                using (var child = Process.GetProcessById(id))
                    return !child.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        static void Reject(byte[] bytes)
        {
            bool rejected = false;
            try
            {
                using (var input = new MemoryStream(bytes))
                    ClipboardSnapshot.Decode(input);
            }
            catch (Exception)
            {
                rejected = true;
            }

            Check(rejected, "malformed data is not a clipboard backup");
        }

        public static int Run()
        {
            checks = 0;
            uint textSequence = 41;
            string textFailure = null;
            ClipboardSnapshot.UnicodeReader textReader = delegate (uint seq, int bound, out string text)
            {
                text = "";
                return textFailure == null;
            };
            Check(ClipboardSnapshot.ReadText(() => textSequence, textReader, () => textFailure) == "", "readable empty Unicode remains available");
            textFailure = "Unicode payload unavailable";
            Check(ClipboardSnapshot.ReadText(() => textSequence, textReader, () => textFailure) == null, "nontext clipboard is distinct from provider failure");
            textFailure = "clipboard busy";
            bool textRefused = false;
            try
            {
                ClipboardSnapshot.ReadText(() => textSequence, textReader, () => textFailure);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                textRefused = true;
            }

            Check(textRefused, "failed native read never masquerades as readable empty data");
            textRefused = false;
            try
            {
                ClipboardSnapshot.ReadText(() => textSequence, delegate (uint seq, int bound, out string text)
                {
                    textSequence++;
                    text = "older";
                    return true;
                }, () => null);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                textRefused = true;
            }

            Check(textRefused, "a newer clipboard invalidates native text too");
            int sourceReads = 0, waits = 0;
            var readySource = Sample();
            var delayedSource = ClipboardSnapshot.ReadSource(() => ++sourceReads < 3 ? new DataObject() : readySource, () => 41u, 41, () => false, format => false, milliseconds =>
            {
                Check(milliseconds == 20, "clipboard view retry has a short fixed delay");
                waits++;
            });
            Check(sourceReads == 3 && waits == 2 && delayedSource.GetFormats(false).Length == 4, "a transient empty OLE view is retried without discarding any native clipboard data");
            int refusalCode = 0;
            sourceReads = waits = 0;
            try
            {
                ClipboardSnapshot.ReadSource(() =>
                {
                    sourceReads++;
                    return null;
                }, () => 41u, 41, () => false, format => false, milliseconds => waits++);
            }
            catch (ClipboardSnapshot.Refused error)
            {
                refusalCode = error.Code;
            }

            Check(refusalCode == 3 && sourceReads == 3 && waits == 2, "an unverified empty view is refused after bounded retries rather than becoming an empty backup");
            var genuinelyEmpty = ClipboardSnapshot.ReadSource(() => null, () => 41u, 41, () => true, format => false, milliseconds =>
            {
                throw new Exception("unnecessary empty retry");
            });
            Check(genuinelyEmpty.GetFormats(false).Length == 0, "a natively verified empty clipboard remains a valid zero-format backup");
            uint changing = 41;
            refusalCode = 0;
            try
            {
                ClipboardSnapshot.ReadSource(() =>
                {
                    changing++;
                    return readySource;
                }, () => changing, 41, () => false, format => false, milliseconds =>
                {
                    throw new Exception("new clipboard must not retry");
                });
            }
            catch (ClipboardSnapshot.Refused error)
            {
                refusalCode = error.Code;
            }

            Check(refusalCode == 5, "a clipboard change while acquiring the view cannot be accepted or retried as the old clipboard");
            bool cleared = false, set = false;
            Check(ClipboardSnapshot.RestoreData(new DataObject(), () =>
            {
                cleared = true;
                return true;
            }, data => set = true) && cleared && !set, "a zero-format backup restores genuine emptiness instead of publishing an empty OLE data object");
            Check(!ClipboardSnapshot.RestoreData(new DataObject(), () => false, data =>
            {
                throw new Exception("unsafe empty restore fallback");
            }), "failed ownership-checked clearing never falls back to replacing a newer clipboard");
            var explicitEmptyText = new DataObject();
            explicitEmptyText.SetData(DataFormats.UnicodeText, false, "");
            set = false;
            Check(ClipboardSnapshot.RestoreData(explicitEmptyText, () =>
            {
                throw new Exception("explicit text must retain its format");
            }, data => set = ReferenceEquals(data, explicitEmptyText)) && set, "an explicitly empty text format is retained rather than cleared as no data");
            byte[] encoded;
            using (var output = new MemoryStream())
            {
                ClipboardSnapshot.Encode(output, new ClipboardSnapshot(Sample(), 41));
                encoded = output.ToArray();
            }

            ClipboardSnapshot snapshot;
            using (var input = new MemoryStream(encoded))
                snapshot = ClipboardSnapshot.Decode(input);
            Check(snapshot.Sequence == 41 && snapshot.Text == "Test \ud800 text कṛṣṇa\r\n", "UTF-16 units and sequence survive without encoder fallback");
            Check(snapshot.Data.GetFormats(false).Length == 4, "all advertised formats are preserved");
            Check(((byte[])snapshot.Data.GetData("snapshot-bytes", false)).SequenceEqual(new byte[] { 1, 0, 255 }), "opaque bytes are preserved");
            Check(((MemoryStream)snapshot.Data.GetData("HTML Format", false)).ToArray().SequenceEqual(new byte[] { 9, 8, 7 }), "rich-text stream bytes are preserved");
            Check(((string[])snapshot.Data.GetData(DataFormats.FileDrop, false)).SequenceEqual(new[] { "first", "second" }), "file-drop strings are preserved");
            using (var bitmap = new Bitmap(2, 2))
            using (var bytes = new MemoryStream())
            {
                bitmap.SetResolution(120, 144);
                bitmap.SetPixel(1, 1, Color.FromArgb(127, 3, 10, 200));
                var data = new DataObject();
                data.SetData(DataFormats.Bitmap, false, bitmap);
                ClipboardSnapshot.Encode(bytes, new ClipboardSnapshot(data, 41));
                bytes.Position = 0;
                using (var copy = (Bitmap)ClipboardSnapshot.Decode(bytes).Data.GetData(DataFormats.Bitmap, false))
                {
                    Check(copy.Size == bitmap.Size && copy.GetPixel(1, 1).ToArgb() == bitmap.GetPixel(1, 1).ToArgb(), "bitmap dimensions, pixels and alpha survive");
                    Check(Math.Abs(copy.HorizontalResolution - 120) < 0.1 && Math.Abs(copy.VerticalResolution - 144) < 0.1, "bitmap DPI survives");
                }
            }

            var unsupported = new DataObject();
            unsupported.SetData("unsupported", false, new object ());
            bool refused = false;
            try
            {
                using (var output = new MemoryStream())
                    ClipboardSnapshot.Encode(output, new ClipboardSnapshot(unsupported, 41));
            }
            catch (InvalidDataException)
            {
                refused = true;
            }

            Check(refused, "CLR/COM object deserialization is never a wire fallback");
            foreach (int length in new[]
            {
                0,
                1,
                15,
                encoded.Length - 1
            }

            )
                Reject(encoded.Take(length).ToArray());
            var badHeader = (byte[])encoded.Clone();
            badHeader[0] ^= 1;
            Reject(badHeader);
            var badVersion = (byte[])encoded.Clone();
            badVersion[4] = 2;
            Reject(badVersion);
            var badCount = (byte[])encoded.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(129), 0, badCount, 12, 4);
            Reject(badCount);
            var badLength = (byte[])encoded.Clone();
            Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue), 0, badLength, 16, 4);
            Reject(badLength);
            Reject(encoded.Concat(new byte[] { 0 }).ToArray());
            using (var bytes = new MemoryStream())
            {
                var one = new DataObject();
                one.SetData("X", false, "Y");
                ClipboardSnapshot.Encode(bytes, new ClipboardSnapshot(one, 41));
                byte[] wrongType = bytes.ToArray();
                wrongType[22] = 255;
                Reject(wrongType);
                byte[] negative = bytes.ToArray();
                Buffer.BlockCopy(BitConverter.GetBytes(-1), 0, negative, 23, 4);
                Reject(negative);
                byte[] duplicate = bytes.ToArray().Concat(bytes.ToArray().Skip(16)).ToArray();
                Buffer.BlockCopy(BitConverter.GetBytes(2), 0, duplicate, 12, 4);
                Reject(duplicate);
            }

            foreach (string mode in new[]
            {
                "good",
                "exit",
                "stall",
                "bad",
                "wrong",
                "changed"
            }

            )
            {
                int pid = 0, reads = 0;
                var clock = Stopwatch.StartNew();
                var result = ClipboardSnapshot.Capture(() =>
                {
                    var child = Start(mode == "changed" ? "good" : mode);
                    pid = child.Id;
                    return child;
                }, () => mode == "changed" && Interlocked.Increment(ref reads) > 1 ? 42u : 41u, mode == "stall" ? 200 : 2500);
                Check(mode == "good" ? result != null && result.Text == snapshot.Text : result == null, "child outcome is handled without affecting the caller: " + mode);
                Check(!Alive(pid), "only the owned capture child is cleaned up: " + mode);
                if (mode == "stall")
                    Check(clock.ElapsedMilliseconds < 1500, "provider hang has a bounded deadline");
            }

            var failures = new ClipboardCaptureFailures();
            int attempts = 0;
            Func<string, Process> attempt = mode =>
            {
                attempts++;
                return Start(mode);
            };
            var priorTrace = ClipboardSnapshot.Trace;
            try
            {
                ClipboardSnapshot.Trace = message =>
                {
                    throw new InvalidOperationException("diagnostic sink failed");
                };
                Check(ClipboardSnapshot.Capture(() => attempt("exit"), () => 41u, 2500, failures) == null, "provider death is contained even if diagnostics fail");
                Check(ClipboardSnapshot.Capture(() => attempt("good"), () => 41u, 2500, failures) == null && attempts == 1, "unchanged bad clipboard does not create a loop of crashing helpers");
                Check(ClipboardSnapshot.Capture(() => attempt("wrong"), () => 42u, 2500, failures) != null && attempts == 2, "new clipboard data permits capture again without restarting the caller");
            }
            finally
            {
                ClipboardSnapshot.Trace = priorTrace;
            }

            // A valid frame is not success until its owned helper exits cleanly.
            // Faults after closing stdout used to bypass the unchanged-sequence
            // failure cache and launch another crashing child on every request.
            foreach (string mode in new[]
            {
                "complete-exit",
                "complete-stall"
            }

            )
            {
                var lateFailures = new ClipboardCaptureFailures();
                int lateAttempts = 0, latePid = 0;
                Func<Process> lateStart = () =>
                {
                    lateAttempts++;
                    var child = Start(mode);
                    latePid = child.Id;
                    return child;
                };
                Check(ClipboardSnapshot.Capture(lateStart, () => 41u, 2500, lateFailures) == null, "completed frame with unsuccessful exit is rejected: " + mode);
                Check(!Alive(latePid), "late failed child is cleaned up: " + mode);
                Check(ClipboardSnapshot.Capture(lateStart, () => 41u, 2500, lateFailures) == null && lateAttempts == 1, "late provider failure blocks another child for unchanged data: " + mode);
                Check(ClipboardSnapshot.Capture(() => Start("wrong"), () => 42u, 2500, lateFailures) != null, "new clipboard sequence permits capture after late failure: " + mode);
            }

            var unsupportedFailures = new ClipboardCaptureFailures();
            Check(ClipboardSnapshot.Capture(() => Start("unsupported"), () => 41u, 2500, unsupportedFailures) == null, "normally unsupported data is declined");
            Check(ClipboardSnapshot.Capture(() => Start("good"), () => 41u, 2500, unsupportedFailures) != null, "unsupported data does not poison retries for the current sequence");
            var emptyViewFailures = new ClipboardCaptureFailures();
            Check(ClipboardSnapshot.Capture(() => Start("empty-view"), () => 41u, 2500, emptyViewFailures) == null && ClipboardSnapshot.LastFailure == "clipboard helper could not see the nonempty clipboard", "empty-view refusal is diagnosed without exposing contents");
            Check(ClipboardSnapshot.Capture(() => Start("good"), () => 41u, 2500, emptyViewFailures) != null, "normal coded refusal does not poison later captures of the same sequence");
            var startFailures = new ClipboardCaptureFailures();
            Check(ClipboardSnapshot.Capture(() =>
            {
                throw new IOException("fixture startup failure");
            }, () => 41u, 2500, startFailures) == null, "child startup failure is contained");
            Check(ClipboardSnapshot.Capture(() => Start("good"), () => 41u, 2500, startFailures) != null, "startup failure does not poison retries for the current sequence");
            Console.WriteLine("Clipboard snapshot isolation: " + checks + " assertions; exact typed copies, invalid data, child death, timeout and changed sequence; no system clipboard.");
            return checks;
        }
    }
}
