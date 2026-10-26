// KeyJaya: Read-only shortcut metadata, isolated from the resident and its hooks.
// Never sets CustomizationContext, reads document text, or executes a Word command.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    public sealed class WordShortcutKey
    {
        public string Shortcut, Action, Expected;
        public uint Key, Modifiers;
    }

    public sealed class WordShortcutFinding
    {
        public string Window, Shortcut, Action, Command, Finding, Expected;
        public int Category;
        public bool Review;
    }

    public sealed class WordShortcutReport
    {
        public string Status;
        public bool Complete;
        public int WindowsChecked;
        public List<WordShortcutFinding> Findings = new List<WordShortcutFinding>();
    }

    public static class WordShortcutInspector
    {
        delegate bool WindowVisitor(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")]
        static extern bool EnumWindows(WindowVisitor callback, IntPtr parameter);
        public static WordShortcutKey[] Keys(ShortcutSettings settings)
        {
            var keys = new List<WordShortcutKey>();
            for (int n = 0; n < ShortcutSettings.Roles.Length; n++)
            {
                var binding = settings[ShortcutSettings.Roles[n]];
                if (binding.Tap == null)
                    keys.Add(new WordShortcutKey { Shortcut = binding.Text, Action = ShortcutSettings.Labels[n], Key = binding.Key, Modifiers = binding.Modifiers });
            }

            foreach (var item in new[]
            {
                new WordShortcutKey
                {
                    Shortcut = "Ctrl+Shift+Left",
                    Key = 37,
                    Modifiers = 6,
                    Expected = "WordLeftExtend"
                },
                new WordShortcutKey
                {
                    Shortcut = "Ctrl+Shift+Right",
                    Key = 39,
                    Modifiers = 6,
                    Expected = "WordRightExtend"
                },
                new WordShortcutKey
                {
                    Shortcut = "Ctrl+Left",
                    Key = 37,
                    Modifiers = 2,
                    Expected = "WordLeft"
                },
                new WordShortcutKey
                {
                    Shortcut = "Ctrl+Right",
                    Key = 39,
                    Modifiers = 2,
                    Expected = "WordRight"
                },
                new WordShortcutKey
                {
                    Shortcut = "Shift+Left",
                    Key = 37,
                    Modifiers = 4,
                    Expected = "CharLeftExtend"
                },
                new WordShortcutKey
                {
                    Shortcut = "Shift+Right",
                    Key = 39,
                    Modifiers = 4,
                    Expected = "CharRightExtend"
                }
            }

            )
            {
                item.Action = "Word navigation";
                keys.Add(item);
            }

            return keys.ToArray();
        }

        internal static object[] KeyArguments(WordShortcutKey key)
        {
            // WdKey differs from RegisterHotKey: Control=512, Shift=256, Alt=1024.
            var args = new List<object>
            {
                (int)key.Key
            };
            if ((key.Modifiers & 2) != 0)
                args.Add(512);
            if ((key.Modifiers & 4) != 0)
                args.Add(256);
            if ((key.Modifiers & 1) != 0)
                args.Add(1024);
            while (args.Count < 4)
                args.Add(Type.Missing);
            return args.ToArray();
        }

        static object Get(object owner, string member, params object[] args)
        {
            return owner.GetType().InvokeMember(member, BindingFlags.GetProperty | BindingFlags.OptionalParamBinding, null, owner, args);
        }

        static void Release(object value)
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

        internal static WordShortcutFinding Classify(WordShortcutKey key, string command, int category)
        {
            var row = new WordShortcutFinding
            {
                Shortcut = key.Shortcut,
                Action = key.Action,
                Command = command ?? "",
                Expected = key.Expected,
                Category = category
            };
            if (key.Expected != null)
            {
                row.Review = category != 1 || !string.Equals(command, key.Expected, StringComparison.OrdinalIgnoreCase);
                row.Finding = row.Review ? "Different navigation assignment; may be intentional" : "Normal navigation";
            }
            else
            {
                row.Review = category > 0;
                row.Finding = row.Review ? "KeyJaya takes priority while enabled" : category == 0 ? "Disabled in Word; no overlap" : "No Word assignment";
            }

            if (row.Command.Length == 0)
                row.Command = category == 0 ? "Disabled" : category == -1 ? "Unassigned" : category == 7 ? "Word shortcut prefix" : category == 3 ? "Word font assignment" : category == 5 ? "Word style assignment" : category == 6 ? "Word symbol assignment" : category == 4 ? "Word AutoText assignment" : "Assigned in Word";
            return row;
        }

        // Tests use a getter-only fake object model; no Windows input is involved.
        internal static List<WordShortcutFinding> ReadBindings(object app, WordShortcutKey[] keys, Func<bool> stillOwned, Action<object> release)
        {
            var rows = new List<WordShortcutFinding>();
            foreach (var key in keys)
            {
                if (!stillOwned())
                    throw new InvalidOperationException("Word's active window changed. Run the check again.");
                object binding = null;
                try
                {
                    object code = app.GetType().InvokeMember("BuildKeyCode", BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding, null, app, KeyArguments(key));
                    binding = Get(app, "FindKey", code, Type.Missing);
                    string command = Convert.ToString(Get(binding, "Command"));
                    int category = Convert.ToInt32(Get(binding, "KeyCategory"));
                    rows.Add(Classify(key, command, category));
                }
                finally
                {
                    if (binding != null)
                        release(binding);
                }

                if (!stillOwned())
                    throw new InvalidOperationException("Word's active window changed. Run the check again.");
            }

            return rows;
        }

        static bool Owns(object app, IntPtr frame, uint process)
        {
            object window = null;
            try
            {
                window = Get(app, "ActiveWindow");
                IntPtr actual = new IntPtr(Convert.ToInt64(Get(window, "Hwnd")));
                uint actualProcess, currentProcess;
                Native.GetWindowThreadProcessId(actual, out actualProcess);
                Native.GetWindowThreadProcessId(frame, out currentProcess);
                return currentProcess == process && WordApplication.SameWindow(WordPasteOptions.Owner(frame), process, WordPasteOptions.Owner(actual), actualProcess);
            }
            finally
            {
                Release(window);
            }
        }

        public static WordShortcutReport Inspect(ShortcutSettings settings)
        {
            var report = new WordShortcutReport();
            var frames = new List<IntPtr>();
            bool limited = false;
            EnumWindows((window, unused) =>
            {
                var name = new StringBuilder(64);
                Native.GetClassName(window, name, name.Capacity);
                if (Native.IsWindowVisible(window) && name.ToString() == "OpusApp")
                {
                    if (frames.Count == 8)
                    {
                        limited = true;
                        return false;
                    }

                    frames.Add(window);
                }

                return true;
            }, IntPtr.Zero);
            if (frames.Count == 0)
            {
                report.Status = "Open Word with a document, then run the check again. Word was not started or activated.";
                return report;
            }

            var processes = new HashSet<uint>();
            bool unavailable = false;
            foreach (var frame in frames)
            {
                uint process;
                Native.GetWindowThreadProcessId(frame, out process);
                if (process == 0 || processes.Contains(process))
                    continue;
                object app = null;
                try
                {
                    app = WordApplication.ForFrame(frame);
                    if (app == null)
                        continue;
                    var rows = ReadBindings(app, Keys(settings), () => Owns(app, frame, process), Release);
                    processes.Add(process);
                    report.WindowsChecked++;
                    foreach (var row in rows)
                    {
                        row.Window = "Word instance " + report.WindowsChecked + " (process " + process + ")";
                        report.Findings.Add(row);
                    }
                }
                catch (Exception)
                {
                    unavailable = true;
                }
                finally
                {
                    Release(app);
                }
            }

            report.Complete = !limited && !unavailable && report.WindowsChecked > 0 && frames.All(frame =>
            {
                uint process;
                Native.GetWindowThreadProcessId(frame, out process);
                return processes.Contains(process);
            });
            int overlaps = report.Findings.Count(x => x.Expected == null && x.Review), navigation = report.Findings.Count(x => x.Expected != null && x.Review);
            report.Status = report.WindowsChecked == 0 ? "Word's shortcut assignments could not be verified. Word may be busy; try again." : "Checked the active window in " + report.WindowsChecked + " Word instance(s): " + overlaps + " KeyJaya overlap(s), " + navigation + " navigation assignment(s) to review.";
            if (!report.Complete && report.WindowsChecked > 0)
                report.Status += " Some Word windows could not be checked; this report is incomplete.";
            return report;
        }

        internal static int WriteReport()
        {
            WordShortcutReport report;
            try
            {
                using (var input = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8, true))
                {
                    char[] buffer = new char[16385];
                    int count = 0, read;
                    while (count < buffer.Length && (read = input.Read(buffer, count, buffer.Length - count)) > 0)
                        count += read;
                    if (count == buffer.Length)
                        throw new InvalidDataException("Shortcut request is too large.");
                    var values = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(new string (buffer, 0, count));
                    if (values == null)
                        throw new InvalidDataException("Missing shortcut request.");
                    report = Inspect(new ShortcutSettings(null, values));
                }
            }
            catch (Exception)
            {
                report = new WordShortcutReport
                {
                    Status = "Word's shortcut assignments could not be read. Try again when Word is ready."
                };
            }

            using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)))
                output.Write(new JavaScriptSerializer().Serialize(report));
            return 0;
        }

        public static WordShortcutReport Check(ShortcutSettings settings)
        {
            // A hung Word COM call is confined to this disposable helper process.
            // The parent never waits on Word or releases its COM objects itself.
            using (var child = new Process())
            {
                child.StartInfo = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath, "--check-word-shortcuts")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8
                };
                try
                {
                    child.Start();
                    var output = child.StandardOutput.ReadToEndAsync();
                    child.StandardInput.Write(new JavaScriptSerializer().Serialize(ShortcutSettings.Roles.ToDictionary(role => role, role => settings[role].Text)));
                    child.StandardInput.Close();
                    if (!child.WaitForExit(5000))
                    {
                        child.Kill();
                        child.WaitForExit(500);
                        return new WordShortcutReport
                        {
                            Status = "Word did not answer within five seconds. No assignments were changed; try again when Word is ready."
                        };
                    }

                    if (!output.Wait(500) || output.Result.Length > 65536)
                        throw new IOException("Shortcut report unavailable.");
                    var report = new JavaScriptSerializer().Deserialize<WordShortcutReport>(output.Result);
                    if (report == null)
                        throw new IOException("Missing shortcut report.");
                    return report;
                }
                catch (Exception)
                {
                    try
                    {
                        if (!child.HasExited)
                            child.Kill();
                    }
                    catch (Exception)
                    {
                    }

                    return new WordShortcutReport
                    {
                        Status = "The Word shortcut check could not finish. No assignments were changed."
                    };
                }
            }
        }
    }
}
