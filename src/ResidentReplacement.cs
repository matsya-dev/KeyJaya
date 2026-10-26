// KeyJaya: Pure ownership predicate used when setup/uninstall identifies an earlier resident.
// This helper does not terminate processes; callers control that side effect.
using System;
using System.IO;

namespace KeyJaya
{
    public static class ResidentReplacement
    {
        public static bool Owns(string executable, string product, int session, int installerSession, params string[] roots)
        {
            if (session != installerSession || string.IsNullOrEmpty(executable))
                return false;
            string file = Path.GetFullPath(executable);
            foreach (string root in roots)
                if (!string.IsNullOrEmpty(root) && file.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            return Path.GetFileName(file).Equals("keyjaya-app.exe", StringComparison.OrdinalIgnoreCase) && product == "KeyJaya";
        }
    }
}
