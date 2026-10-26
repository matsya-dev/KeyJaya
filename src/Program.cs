// KeyJaya: Process entry point: command-line conversion, reference windows, and the single resident instance.
// Mode dispatch happens before the resident is started; loading conversion classes does not run this entry point.
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace KeyJaya
{
    public static class Program
    {
        // GUI executables may have redirected streams without an attached console.
        // Read/write UTF-8 streams directly instead of changing the console code page.
        static int ConvertInput(string root, string mode, bool itrans)
        {
            using (Core core = new Core(root))
            using (var input = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8, true))
            using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)))
            {
                string text = input.ReadToEnd();
                output.Write(core.ConvertSource(text, mode, itrans ? "itrans" : "auto"));
            }

            return 0;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            string root = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
            if (args.Length > 1 && args[0] == "--root")
                root = Path.GetFullPath(args[1]);
            if (args.Length > 0 && args[0] == "--version")
            {
                Console.WriteLine(Version.Current);
                return 0;
            }

            if (args.Length > 0 && args[0] == "--convert")
                return ConvertInput(root, args.Length > 1 ? args[1] : "unicode", false);
            if (args.Length > 0 && args[0] == "--convert-itrans")
                return ConvertInput(root, args.Length > 1 ? args[1] : "unicode", true);
            if (args.Length > 0 && args[0] == "--cheat-sheet")
            {
                Application.EnableVisualStyles();
                Native.DpiAware();
                Application.SetCompatibleTextRenderingDefault(false);
                Action<int> request = message =>
                {
                    long owner;
                    if (args.Length > 1 && long.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out owner) && owner != 0)
                        Native.PostMessage(new IntPtr(owner), (uint)message, new IntPtr(Process.GetCurrentProcess().Id), IntPtr.Zero);
                };
                using (Core reference = new Core(root))
                using (var window = new CheatSheetWindow(reference.ActiveShortTriggers(), new ShortcutSettings(Path.Combine(Configuration.Folder(root), "shortcuts.ini")), args.Length > 1 ? (Action)(() => request(Resident.ShortcutRequestMessage)) : null, args.Length > 1 ? (Action)(() => request(Resident.TriggerEditorRequestMessage)) : null))
                    Application.Run(window);
                return 0;
            }

            if (args.Length > 0 && args[0] == "--tray-preview")
            {
                Application.EnableVisualStyles();
                Native.DpiAware();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var preview = new Resident(root, true, true))
                {
                    IntPtr handle = preview.Handle;
                    Application.Run(preview);
                }

                return 0;
            }

            bool probe = Array.IndexOf(args, "--probe") >= 0;
            Application.EnableVisualStyles();
            Native.DpiAware();
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\KeyJayaResident", out created))
            {
                if (!created)
                {
                    if (Array.IndexOf(args, LaunchRequests.CheatSheetArgument) >= 0 && !LaunchRequests.Request(root))
                    {
                        MessageBox.Show("The cheat sheet could not open. Please use Cheat sheet in the KeyJaya tray menu.", "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return 1;
                    }

                    return 0;
                }

                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    using (Resident app = new Resident(root, probe, false, Array.IndexOf(args, "--diagnostic") >= 0))
                    {
                        IntPtr handle = app.Handle;
                        if (Array.IndexOf(args, LaunchRequests.CheatSheetArgument) >= 0)
                            app.BeginInvoke((Action)app.OpenCheatSheet);
                        System.Windows.Forms.Timer stop = null;
                        if (probe)
                        {
                            stop = new System.Windows.Forms.Timer
                            {
                                Interval = 30000
                            };
                            stop.Tick += (s, e) => app.Close();
                            stop.Start();
                        }

                        Application.Run(app);
                        if (stop != null)
                            stop.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    if (probe)
                    {
                        File.WriteAllText(Path.Combine(root, "probe-error.txt"), ex.ToString());
                        return 1;
                    }

                    MessageBox.Show("KeyJaya could not start: " + ex.Message, "KeyJaya", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }
            }

            return 0;
        }
    }
}
