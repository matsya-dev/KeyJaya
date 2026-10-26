// Packaging/upgrade policy checks in disposable files. Does not run an installer,
// update the installed app, register startup, or open a native document.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KeyJaya
{
    public static class DistributionTests
    {
        static int passed;
        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Distribution policy: " + message);
            passed++;
        }

        static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }

        static Dictionary<string, byte[]> Snapshot(string root)
        {
            return Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(path => path.Substring(root.Length + 1), File.ReadAllBytes);
        }

        static void FailedRetirement(string scratch, string[] excluded)
        {
            // FileShare.Read permits the transaction's backup but denies Delete.
            // Exercise a real filesystem failure at every retirement position,
            // including after some files and migration markers have changed.
            for (int failure = 0; failure < excluded.Length; failure++)
            {
                string root = Path.Combine(scratch, "failure-" + failure), stage = root + "-stage";
                foreach (string relative in excluded)
                    Write(root, relative, "previous " + relative);
                Write(root, "config/short-triggers.yml", "user trigger edits\r\n");
                Write(root, "config/settings.ini", "encoding_override=balaram\r\n");
                Write(root, "state/existing.flag", "old migration marker");
                Write(stage, "config/short-triggers.yml", "staged migration");
                Write(stage, "state/existing.flag", "updated marker");
                Write(stage, "state/new.flag", "new migration marker");
                var before = Snapshot(root);
                var transaction = new InstallTransaction(root, stage);
                using (var locked = new FileStream(Path.Combine(root, excluded[failure].Replace('/', Path.DirectorySeparatorChar)), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    transaction.Apply();
                    bool failed = false;
                    try
                    {
                        for (int n = 0; n < excluded.Length; n++)
                        {
                            transaction.Retire(excluded[n].Replace('/', Path.DirectorySeparatorChar));
                            Check(!File.Exists(Path.Combine(root, excluded[n].Replace('/', Path.DirectorySeparatorChar))), "retirement committed before injected lock failure");
                        }
                    }
                    catch (IOException)
                    {
                        failed = true;
                    }

                    Check(failed, "locked retirement fails at position " + failure);
                    transaction.Rollback();
                }

                var after = Snapshot(root);
                Check(before.Count == after.Count && before.All(row => after.ContainsKey(row.Key) && row.Value.SequenceEqual(after[row.Key])), "failed retirement restores configuration, marker inventory and all retired files at position " + failure);
                Check(before.All(row => File.ReadAllBytes(Path.Combine(transaction.Backup, row.Key)).SequenceEqual(row.Value)), "failure backup retains every original byte at position " + failure);
            }
        }

        public static int Run(string scratch, string policy)
        {
            passed = 0;
            Directory.CreateDirectory(scratch);
            string[] excluded = DistributionFiles.Read(policy);
            Check(excluded.Length > 0 && excluded.All(DistributionFiles.Valid), "complete source-only manifest accepted");
            foreach (string invalid in new[]
            {
                "LICENSE",
                "config/short-triggers.yml",
                "engine/keyjaya-app.exe",
                "../outside.txt",
                "data/../config/settings.ini",
                "data/conversion.dat",
                "data/conversion.IDX",
                "data/english.hash",
                "data/words.tsv",
                "docs/../../README.md",
                "data/..",
                "data/name\tbad.txt",
                "data/C:notice.txt",
                "data/notice?.txt",
                "data/conversion.dat.",
                "data/ENGLIS~1.HAS"
            }

            )
                Check(!DistributionFiles.Valid(invalid), "protected or escaping path rejected: " + invalid);
            string legacy = Path.Combine(scratch, "legacy"), stage = Path.Combine(scratch, "stage");
            foreach (string relative in excluded)
                Write(legacy, relative, "previous factory file " + relative);
            Write(legacy, "LICENSE", "KeyJaya original copyright and MIT notice");
            Write(legacy, "config/short-triggers.yml", "user edits and disabled triggers");
            Write(legacy, "config/settings.ini", "user encoding and shortcut preferences");
            Write(legacy, "data/conversion.dat", "runtime data preserved");
            Write(legacy, "data/conversion.idx", "runtime index preserved");
            Write(legacy, "engine/keyjaya-app.exe", "previous engine");
            Write(stage, "engine/keyjaya-app.exe", "updated engine");
            Write(stage, "assets/" + DistributionFiles.Manifest, File.ReadAllText(policy));
            var before = Snapshot(legacy);
            var transaction = new InstallTransaction(legacy, stage);
            transaction.Apply();
            foreach (string relative in DistributionFiles.Read(Path.Combine(legacy, "assets", DistributionFiles.Manifest)))
                transaction.Retire(relative.Replace('/', Path.DirectorySeparatorChar));
            foreach (string relative in excluded)
            {
                string native = relative.Replace('/', Path.DirectorySeparatorChar);
                Check(!File.Exists(Path.Combine(legacy, native)), "source-only deployed copy retired");
                Check(File.ReadAllBytes(Path.Combine(transaction.Backup, native)).SequenceEqual(before[native]), "retired file retained in transaction backup");
            }

            foreach (string relative in new[]
            {
                "LICENSE",
                "config/short-triggers.yml",
                "config/settings.ini",
                "data/conversion.dat",
                "data/conversion.idx"
            }

            )
                Check(File.ReadAllBytes(Path.Combine(legacy, relative.Replace('/', Path.DirectorySeparatorChar))).SequenceEqual(before[relative.Replace('/', Path.DirectorySeparatorChar)]), "own notice/user data preserved");
            transaction.Rollback();
            var restored = Snapshot(legacy);
            Check(restored.Count == before.Count && before.All(item => restored.ContainsKey(item.Key) && restored[item.Key].SequenceEqual(item.Value)), "full rollback restores exact original files");
            string invalidPolicy = Path.Combine(scratch, "invalid-policy.txt");
            foreach (string text in new[]
            {
                excluded[0] + "\nconfig/settings.ini\n",
                "data/NOTICE.txt\nDATA/notice.TXT\n"
            }

            )
            {
                File.WriteAllText(invalidPolicy, text);
                bool rejected = false;
                try
                {
                    DistributionFiles.Read(invalidPolicy);
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }

                Check(rejected, "invalid/duplicate policy rejected before returning any retirement plan");
                Check(File.Exists(Path.Combine(legacy, excluded[0].Replace('/', Path.DirectorySeparatorChar))), "failed policy leaves prior content untouched");
            }

            FailedRetirement(scratch, excluded);
            Console.WriteLine("Distribution policy: " + passed + " assertions; exclusions, own notice, configuration and complete rollback.");
            return passed;
        }
    }
}
