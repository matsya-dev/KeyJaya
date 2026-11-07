// Test-only entry point. Never include this source in a production build.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KeyJaya
{
    internal static class WordRecoveryFixture
    {
        internal static bool PrivateProfile(string folder, string requested)
        {
            if (string.IsNullOrWhiteSpace(requested))
                return false;
            try
            {
                string profiles = Path.GetFullPath(Path.Combine(folder, "..", "profiles"));
                string profile = Path.GetFullPath(requested).TrimEnd(Path.DirectorySeparatorChar);
                Guid identity;
                return string.Equals(Path.GetDirectoryName(profile), profiles, StringComparison.OrdinalIgnoreCase) && Guid.TryParseExact(Path.GetFileName(profile), "N", out identity);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal static int Dispatch(string[] args, bool armed, Action silent, Func<string[], int> production)
        {
            if (args.Length == 1 && args[0] == WordFocusMonitor.Argument)
            {
                if (armed)
                {
                    silent();
                    return 0;
                }

                return production(args);
            }

            if (args.Length == 1 && args[0] == ClipboardSnapshot.Argument)
                return production(args);
            if (args.Length == 4 && args[0] == "--fixture-resident" && args[1] == "--root" && !string.IsNullOrWhiteSpace(args[2]) && args[3] == "--diagnostic")
                return production(new[] { args[1], args[2], args[3] });
            return 2;
        }

        static int SelfTest()
        {
            int checks = 0, silence = 0, delegated = 0;
            bool armed = true;
            string[] forwarded = null;
            Action silent = () =>
            {
                silence++;
                armed = false;
            };
            Func<string[], int> production = args =>
            {
                delegated++;
                forwarded = args;
                return 17;
            };
            Action<bool> check = valid =>
            {
                if (!valid)
                    throw new InvalidOperationException("Recovery fixture policy failed");
                checks++;
            };
            foreach (string[] denied in new[]
            {
                new string[0],
                new[]
                {
                    "--not-authorized"
                },
                new[]
                {
                    "--root",
                    "somewhere",
                    "--diagnostic"
                },
                new[]
                {
                    "--fixture-resident",
                    "--root",
                    "somewhere"
                },
                new[]
                {
                    "--fixture-resident",
                    "--root",
                    "",
                    "--diagnostic"
                },
                new[]
                {
                    "--fixture-resident",
                    "--root",
                    "somewhere",
                    "--probe"
                }
            }

            )
                check(Dispatch(denied, armed, silent, production) == 2 && silence == 0 && delegated == 0);
            check(Dispatch(new[] { WordFocusMonitor.Argument }, armed, silent, production) == 0 && silence == 1 && delegated == 0 && !armed);
            check(Dispatch(new[] { WordFocusMonitor.Argument }, armed, silent, production) == 17 && silence == 1 && delegated == 1 && forwarded[0] == WordFocusMonitor.Argument);
            check(Dispatch(new[] { ClipboardSnapshot.Argument }, true, silent, production) == 17 && silence == 1 && delegated == 2);
            check(Dispatch(new[] { "--fixture-resident", "--root", "prepared-private-profile", "--diagnostic" }, true, silent, production) == 17 && silence == 1 && delegated == 3 && forwarded.Length == 3 && forwarded[0] == "--root" && forwarded[1] == "prepared-private-profile" && forwarded[2] == "--diagnostic");
            string folder = Path.Combine(Path.GetTempPath(), "KeyJayaWordPrep", "build");
            string profile = Path.GetFullPath(Path.Combine(folder, "..", "profiles", "0123456789abcdef0123456789abcdef"));
            check(PrivateProfile(folder, profile));
            check(!PrivateProfile(folder, Path.Combine(folder, "..", "word-speed-trial", "profile")));
            check(!PrivateProfile(folder, Path.Combine(profile, "nested")) && !PrivateProfile(folder, Path.Combine(profile, "..")));
            check(!PrivateProfile(folder, "") && !PrivateProfile(folder, null));
            check(!PrivateProfile(folder, Path.Combine(folder, "..", "profiles", "installed-profile")));
            Console.WriteLine("PASS: " + checks + " recovery-fixture checks; no resident, helper, file, desktop or clipboard operations.");
            return 0;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--self-test")
            {
                try
                {
                    return SelfTest();
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error.Message);
                    return 1;
                }
            }

            string folder = AppDomain.CurrentDomain.BaseDirectory;
            string flag = Path.Combine(folder, "silent-focus-once.flag");
            if (args.Length == 4 && args[0] == "--fixture-resident")
            {
                if (!PrivateProfile(folder, args[2]) || !Directory.Exists(args[2]))
                    return 2;
                var profile = new DirectoryInfo(Path.GetFullPath(args[2]));
                if ((profile.Attributes & FileAttributes.ReparsePoint) != 0 || (profile.Parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    return 2;
            }

            bool armed = args.Length == 1 && args[0] == WordFocusMonitor.Argument && File.Exists(flag);
            return Dispatch(args, armed, () =>
            {
                File.Delete(flag);
                File.WriteAllText(Path.Combine(folder, "silent-focus-helper.pid"), Process.GetCurrentProcess().Id.ToString());
                using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)))
                {
                    output.AutoFlush = true;
                    output.WriteLine("ready");
                    using (var pipe = Console.OpenStandardInput())
                        Program.WaitForParent(pipe);
                }
            }, Program.Main);
        }
    }
}
