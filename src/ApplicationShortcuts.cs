// KeyJaya: Owned Start-menu and desktop shortcuts, including reversible installation changes.
// A shortcut is replaced only after its target is verified; unrelated links are preserved.
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace KeyJaya
{
    public static class ApplicationShortcuts
    {
        public const string FileName = "KeyJaya.lnk";
        public static string ProgramsFolder
        {
            get
            {
                return Path.Combine(Special(Environment.SpecialFolder.Programs), "KeyJaya");
            }
        }

        public static string StartMenuPath
        {
            get
            {
                return Path.Combine(ProgramsFolder, FileName);
            }
        }

        public static string DesktopPath
        {
            get
            {
                return Path.Combine(Special(Environment.SpecialFolder.DesktopDirectory), FileName);
            }
        }

        static string Special(Environment.SpecialFolder folder)
        {
            string value = Environment.GetFolderPath(folder);
            if (string.IsNullOrWhiteSpace(value) || !Path.IsPathRooted(value))
                throw new IOException("Windows could not locate the shortcut folder.");
            return value;
        }

        public static string Executable(string root)
        {
            return Path.Combine(Path.GetFullPath(root), "engine", "keyjaya-app.exe");
        }

        public sealed class Info
        {
            public string Target, Arguments, WorkingDirectory, Icon, Description;
            public int IconIndex;
        }

        [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
        class ShellLink
        {
        }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, IntPtr data, uint flags);
            void GetIDList(out IntPtr list);
            void SetIDList(IntPtr list);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string text);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string path);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder text, int count);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string text);
            void GetHotkey(out short key);
            void SetHotkey(short key);
            void GetShowCmd(out int command);
            void SetShowCmd(int command);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int count, out int index);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);
            void Resolve(IntPtr owner, uint flags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
        }

        static void NoLink(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Shortcut links must be managed manually: " + path);
        }

        public static Info Read(string path)
        {
            NoLink(path);
            object instance = new ShellLink();
            try
            {
                ((IPersistFile)instance).Load(path, 0);
                var link = (IShellLinkW)instance;
                var target = new StringBuilder(32768);
                var working = new StringBuilder(32768);
                var icon = new StringBuilder(32768);
                var args = new StringBuilder(32768);
                var description = new StringBuilder(2048);
                int index;
                link.GetPath(target, target.Capacity, IntPtr.Zero, 4);
                link.GetWorkingDirectory(working, working.Capacity);
                link.GetIconLocation(icon, icon.Capacity, out index);
                link.GetArguments(args, args.Capacity);
                link.GetDescription(description, description.Capacity);
                return new Info
                {
                    Target = target.ToString(),
                    WorkingDirectory = working.ToString(),
                    Icon = icon.ToString(),
                    IconIndex = index,
                    Arguments = args.ToString(),
                    Description = description.ToString()
                };
            }
            finally
            {
                Marshal.FinalReleaseComObject(instance);
            }
        }

        static bool SamePath(string a, string b)
        {
            return !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && Path.GetFullPath(Environment.ExpandEnvironmentVariables(a)).Equals(Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }

        public static bool OwnedBy(string path, params string[] roots)
        {
            if (!File.Exists(path))
                return false;
            var link = Read(path);
            return roots.Any(root => !string.IsNullOrEmpty(root) && SamePath(link.Target, Executable(root)));
        }

        public static byte[] Write(string path, string root)
        {
            path = Path.GetFullPath(path);
            root = Path.GetFullPath(root);
            if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Windows shortcuts require a .lnk filename.");
            if (File.Exists(path))
                NoLink(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N") + ".lnk";
            object instance = new ShellLink();
            try
            {
                var link = (IShellLinkW)instance;
                link.SetPath(Executable(root));
                link.SetArguments("");
                link.SetWorkingDirectory(root);
                link.SetIconLocation(Path.Combine(root, "assets", "keyjaya.ico"), 0);
                link.SetDescription("KeyJaya — Sanskrit and Bengali typing and transliteration");
                link.SetShowCmd(1);
                ((IPersistFile)instance).Save(temporary, true);
                byte[] bytes = File.ReadAllBytes(temporary);
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
                return bytes;
            }
            finally
            {
                Marshal.FinalReleaseComObject(instance);
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        public static bool RemoveOwned(string path, string root)
        {
            if (!OwnedBy(path, root))
                return false;
            File.Delete(path);
            return true;
        }

        internal static void RestoreBytes(string path, byte[] bytes)
        {
            NoLink(path);
            string temporary = path + ".restore-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Replace(temporary, path, null);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }
    }

    // Shell locations lie outside the application transaction. Save exact original
    // bytes and roll back only our own writes if the rest of setup fails.
    public sealed class ShortcutTransaction
    {
        readonly string root;
        readonly string[] priorRoots;
        readonly List<string> order = new List<string>(), created = new List<string>();
        readonly Dictionary<string, byte[]> before = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase), written = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        public ShortcutTransaction(string root, params string[] priorRoots)
        {
            this.root = Path.GetFullPath(root);
            this.priorRoots = new[]
            {
                this.root
            }.Concat(priorRoots).ToArray();
        }

        void DirectoryFor(string path)
        {
            if (Directory.Exists(path))
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Shortcut folder is a link: " + path);
                return;
            }

            DirectoryFor(Path.GetDirectoryName(path));
            Directory.CreateDirectory(path);
            created.Add(path);
        }

        public void Apply(string path)
        {
            path = Path.GetFullPath(path);
            if (before.ContainsKey(path))
                throw new InvalidOperationException("Shortcut already staged");
            if (File.Exists(path) && !ApplicationShortcuts.OwnedBy(path, priorRoots))
                throw new IOException("An unrelated shortcut already uses the name KeyJaya: " + path);
            DirectoryFor(Path.GetDirectoryName(path));
            before[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            order.Add(path);
            written[path] = ApplicationShortcuts.Write(path, root);
        }

        public void Rollback()
        {
            var errors = new List<Exception>();
            foreach (string path in order.AsEnumerable().Reverse())
                try
                {
                    if (!written.ContainsKey(path))
                        continue;
                    if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || !File.ReadAllBytes(path).SequenceEqual(written[path]))
                        throw new IOException("Shortcut changed during setup and was left untouched: " + path);
                    if (before[path] == null)
                        File.Delete(path);
                    else
                        ApplicationShortcuts.RestoreBytes(path, before[path]);
                    written.Remove(path);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

            foreach (string path in created.AsEnumerable().Reverse())
                try
                {
                    if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0 && !Directory.EnumerateFileSystemEntries(path).Any())
                        Directory.Delete(path);
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

            if (errors.Count > 0)
                throw new AggregateException("Shortcut rollback needs attention", errors);
        }
    }
}
