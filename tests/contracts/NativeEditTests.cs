// Cross-process native Edit/Rich Edit fixtures, including Acrobat's patternless
// search-control fallback. Hidden owned windows only; no focus, keys or clipboard.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyJaya
{
    internal static class NativeEditTests
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string name);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(uint ex, string cls, string text, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr data);
        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
        static extern IntPtr SetText(IntPtr window, uint message, IntPtr unused, string text);
        [DllImport("user32.dll")]
        static extern bool DestroyWindow(IntPtr window);
        static int assertions;
        static void Check(bool value, string label)
        {
            if (!value)
                throw new Exception("Native edit: " + label);
            assertions++;
        }

        internal static int Host()
        {
            LoadLibrary("msftedit.dll");
            using (var resumed = new ManualResetEvent(false))
            using (var form = new Form())
            {
                IntPtr edit = CreateWindowEx(0, "Edit", "", 0x40000004, 0, 0, 400, 100, form.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                IntPtr rich = CreateWindowEx(0, "RICHEDIT50W", "", 0x40000004, 0, 0, 400, 100, form.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (edit == IntPtr.Zero || rich == IntPtr.Zero)
                    return 1;
                Console.WriteLine(edit.ToInt64() + "," + rich.ToInt64() + "," + form.Handle.ToInt64());
                Console.Out.Flush();
                var reader = new Thread(() =>
                {
                    var input = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding, true);
                    string command;
                    while ((command = input.ReadLine()) != null)
                    {
                        if (command == "pause")
                        {
                            resumed.Reset();
                            form.BeginInvoke((Action)(() =>
                            {
                                Console.WriteLine("paused");
                                Console.Out.Flush();
                                resumed.WaitOne(3000);
                            }));
                        }
                        else if (command == "resume")
                            resumed.Set();
                        else
                            break;
                    }

                    resumed.Set();
                    form.BeginInvoke((Action)Application.ExitThread);
                })
                {
                    IsBackground = true
                };
                reader.Start();
                try
                {
                    Application.Run();
                }
                finally
                {
                    DestroyWindow(edit);
                    DestroyWindow(rich);
                }
            }

            return 0;
        }

        static string Line(Process child)
        {
            var read = Task.Factory.StartNew(() => child.StandardOutput.ReadLine());
            if (!read.Wait(5000) || read.Result == null)
                throw new Exception("Native edit host did not respond.");
            return read.Result;
        }

        internal static int Run()
        {
            assertions = 0;
            object emptyPattern = new object ();
            int patternReads = 0;
            Func<object> accessible = () =>
            {
                patternReads++;
                return emptyPattern;
            };
            foreach (string image in new[]
            {
                "Acrobat.exe",
                "AcroRd32.exe",
                "ACROBAT.EXE"
            }

            )
                foreach (string owner in new[]
                {
                    "AcrobatSDIWindow",
                    "AVL_AVWindow"
                }

                )
                {
                    bool native = SafeEdit.NativeAcrobatEdit(image, owner, "Edit", true, true);
                    Check(SafeEdit.TextProvider(native, accessible) == null && patternReads == 0, "both Acrobat search hosts bypass an advertised empty text provider");
                    Check(SafeEdit.TextProvider(native, () =>
                    {
                        throw new InvalidOperationException();
                    }) == null, "unreliable text-provider discovery cannot block verified native discovery");
                }

            foreach (var facts in new[]
            {
                new[]
                {
                    "WINWORD.EXE",
                    "OpusApp",
                    "Edit"
                },
                new[]
                {
                    "InDesign.exe",
                    "AVL_AVWindow",
                    "Edit"
                },
                new[]
                {
                    "Acrobat.exe",
                    "AcrobatSDIWindow",
                    "RICHEDIT50W"
                },
                new[]
                {
                    "Acrobat.exe",
                    "AcrobatSDIWindow",
                    "AVL_AVView"
                },
                new[]
                {
                    "Acrobat.exe",
                    "#32770",
                    "Edit"
                },
                new[]
                {
                    "other.exe",
                    "AcrobatSDIWindow",
                    "Edit"
                }
            }

            )
            {
                int before = patternReads;
                bool native = SafeEdit.NativeAcrobatEdit(facts[0], facts[1], facts[2], true, true);
                Check(!native && SafeEdit.TextProvider(native, accessible) == emptyPattern && patternReads == before + 1, "other editors, dialogs, PDF views and Acrobat comments keep their existing provider path");
            }

            Check(!SafeEdit.NativeAcrobatEdit("Acrobat.exe", "AcrobatSDIWindow", "Edit", false, true), "wrong native focus/process/root cannot opt into native precedence");
            Check(!SafeEdit.NativeAcrobatEdit("Acrobat.exe", "AVL_AVWindow", "Edit", true, false), "read-only, password or disabled controls cannot opt into native precedence");
            var start = new ProcessStartInfo(Application.ExecutablePath, "--native-edit-host")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };
            using (var child = Process.Start(start))
            {
                try
                {
                    var handles = Line(child).Split(',');
                    Check(handles.Length == 3, "private host controls created");
                    var fields = new[]
                    {
                        new IntPtr(long.Parse(handles[0])),
                        new IntPtr(long.Parse(handles[1]))
                    };
                    foreach (IntPtr field in fields)
                    {
                        uint pid;
                        Native.GetWindowThreadProcessId(field, out pid);
                        Check(pid == (uint)child.Id, "fixture belongs to its own child process");
                        bool rich = field == fields[1];
                        foreach (string source in new[]
                        {
                            "",
                            "krsna",
                            "kṛṣṇa",
                            "bhakti-yoga,",
                            "śāstra and guru",
                            "first\r\nśāstra",
                            "देवनागरी १२३",
                            "বাংলা"
                        }

                        )
                        {
                            Check(SetText(field, 0xC, IntPtr.Zero, source) != IntPtr.Zero, "seed only an owned hidden field");
                            string expected = rich ? source.Replace("\r\n", "\r") : source;
                            string actual;
                            object provider = SafeEdit.TextProvider(!rich, () => emptyPattern);
                            Check((rich || provider == null) && Native.TryReadEditText(field, out actual) && actual == expected, "exact Unicode text even when an empty UIA pattern was advertised");
                            Check(Native.EditTextCoordinates(field, source) == expected, "replacement verification uses native newline coordinates");
                            Check(Native.SelectEditRange(field, 0, expected.Length), "native selection request delivered");
                            int a, b;
                            Native.GetEditSelection(field, out a, out b);
                            Check(a == 0 && b == expected.Length, "exact native selection covers the query");
                            Check(Native.SelectEditRange(field, expected.Length, expected.Length), "caret request delivered");
                            Native.GetEditSelection(field, out a, out b);
                            Check(a == expected.Length && b == expected.Length, "caret coordinates agree with the captured buffer");
                        }
                    }

                    string ignored;
                    Check(!Native.TryReadEditText(new IntPtr(long.Parse(handles[2])), out ignored) && ignored == null, "non-edit host cannot become an editable target");
                    Check(!Native.TryReadEditText(IntPtr.Zero, out ignored) && ignored == null, "invalid handle is unavailable rather than blank");
                    // Exercise the UTF-8 preamble used by CI console hosts even
                    // when this parent runs with an ANSI input encoding.
                    if (child.StandardInput.Encoding.GetPreamble().Length == 0)
                    {
                        byte[] preamble = Encoding.UTF8.GetPreamble();
                        child.StandardInput.BaseStream.Write(preamble, 0, preamble.Length);
                        child.StandardInput.BaseStream.Flush();
                    }

                    child.StandardInput.WriteLine("pause");
                    Check(Line(child) == "paused", "only the owned host is stalled");
                    var clock = Stopwatch.StartNew();
                    Check(!Native.TryReadEditText(fields[0], out ignored) && ignored == null, "unresponsive native field is declined");
                    Check(clock.ElapsedMilliseconds < 1000, "cross-process text read has a bounded timeout");
                    bool failed = false;
                    try
                    {
                        int a, b;
                        Native.GetEditSelection(fields[0], out a, out b);
                    }
                    catch (InvalidOperationException)
                    {
                        failed = true;
                    }

                    Check(failed, "unavailable selection cannot become a caret at zero");
                }
                finally
                {
                    if (!child.HasExited)
                    {
                        child.StandardInput.WriteLine("exit");
                        if (!child.WaitForExit(5000))
                        {
                            child.Kill();
                            child.WaitForExit();
                        }
                    }
                }
            }

            return assertions;
        }
    }
}
