// Opt-in setup orchestration tests against an embedded fixture, not an installer.
// The runner invokes private Install only with test=true in a fresh out/ tree.
// No registration, resident launch, installed profile or native document is used.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KeyJaya
{
    public static class SetupRetirementTests
    {
        static int passed;
        static void Check(bool value, string label)
        {
            if (!value)
                throw new InvalidOperationException(label);
            passed++;
        }

        static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text, new UTF8Encoding(true));
        }

        static Dictionary<string, byte[]> Snapshot(string root)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(path => path.Substring(root.Length + 1), File.ReadAllBytes);
        }

        static object Install(string root, bool fault)
        {
            MethodInfo method = typeof(Setup).GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Static);
            return method.Invoke(null, new object[] { root, null, null, true, fault, false, null });
        }

        static bool Success(object result)
        {
            return (bool)result.GetType().GetField("Success").GetValue(result);
        }

        public static int Run(string source, string scratch)
        {
            source = Path.GetFullPath(source);
            scratch = Path.GetFullPath(scratch);
            Check(scratch.StartsWith(Path.Combine(source, "out") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !Directory.Exists(scratch), "Require a fresh fixture beneath out/");
            Directory.CreateDirectory(scratch);
            string[] obsolete = new[]
            {
                "assets/short-trigger-additions.yml",
                "data/short-triggers.tsv"
            }.Concat(DistributionFiles.Read(Path.Combine(source, "assets", DistributionFiles.Manifest))).ToArray();
            foreach (int failure in new[]
            {
                0,
                obsolete.Length / 2,
                obsolete.Length - 1,
                obsolete.Length
            }

            )
            {
                string root = Path.Combine(scratch, "failure-" + failure);
                foreach (string relative in obsolete)
                    Write(root, relative, "previous obsolete file " + relative);
                Write(root, "config/short-triggers.yml", "# Preserve my edits.\r\nmatches:\r\n  - trigger: 'ACBSP>'\r\n    replace: 'A. C. Bhaktivedanta Swami Prabhupāda'\r\n  - trigger: 'MINE'\r\n    replace: 'My custom text'\r\n");
                Write(root, "config/settings.ini", "encoding_override=balaram\r\nstart_with_windows=false\r\n");
                Write(root, "config/conversion-rules.tsv", "ZZZ\tYYY\r\n");
                Write(root, "state/existing.flag", "existing migration state");
                Write(root, "backups/previous.txt", "existing user backup");
                Write(root, "engine/keyjaya-app.exe", "old fixture engine, never executed");
                Write(root, "VERSION", "previous version");
                var before = Snapshot(root);
                object result;
                if (failure < obsolete.Length)
                {
                    string lockedPath = Path.Combine(root, obsolete[failure].Replace('/', Path.DirectorySeparatorChar));
                    using (var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        result = Install(root, false);
                    string error = (string)result.GetType().GetField("Error").GetValue(result);
                    Check(error.Contains(lockedPath), "Failure reached the expected locked retirement, not an earlier setup phase: " + error);
                }
                else
                {
                    result = Install(root, true);
                    string error = (string)result.GetType().GetField("Error").GetValue(result);
                    Check(error.Contains("Test-only failure after installation swap"), "Failure reached the post-retirement checkpoint");
                }

                Check(!Success(result), "Setup must report failed installation");
                var after = Snapshot(root);
                Check(before.Count == after.Count && before.All(row => after.ContainsKey(row.Key) && row.Value.SequenceEqual(after[row.Key])), "Setup restores exact configuration, markers, obsolete files, engine and backup inventory");
                Check(!File.Exists(Path.Combine(root, "state/sbb-trigger-added.flag")) && !File.Exists(Path.Combine(root, "state/ssr-trigger-added.flag")), "Staged migration markers are removed on rollback");
                string backup = Directory.GetDirectories(scratch, Path.GetFileName(root) + ".backup-*").Single();
                var saved = Snapshot(backup);
                Check(before.Count == saved.Count && before.All(row => saved.ContainsKey(row.Key) && row.Value.SequenceEqual(saved[row.Key])), "Complete recovery backup retains original bytes");
                Check(!after.Keys.Any(path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)), "No staged temporary file is deployed into the restored profile");
            }

            Console.WriteLine("PASS: " + passed + " FAIL: 0; setup retirement and post-swap failure rollback; no installer built or live integration changed.");
            return 0;
        }
    }
}
