// Noninteractive regressions: all eight bindings, conflict validation, and tap/chord dispatch.
// Run: .\test.ps1 -Suite ShortcutSettingsTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace KeyJaya
{
    public static class ShortcutSettingsTests
    {
        public static int Main(string[] args)
        {
            // The production parent runner can be tested without Word: only a
            // disposable child of this test executable handles this command.
            if (args.Length > 0 && args[0] == "--check-word-shortcuts")
            {
                // Match the production helper's explicit UTF-8 protocol. The
                // hosted Windows console may supply a BOM that Console.In
                // exposes as a JSON character instead of consuming it.
                Dictionary<string, string> request;
                using (var input = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8, true))
                    request = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(input.ReadToEnd());
                if (request["word"] == "F24")
                    Thread.Sleep(12000);
                using (var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)))
                {
                    if (request["letter"] == "F23")
                        output.Write("invalid test report");
                    else
                        output.Write(new JavaScriptSerializer().Serialize(new WordShortcutReport { Status = "Isolated test helper", Complete = true, WindowsChecked = 1 }));
                }

                return 0;
            }

            int passed = WordShortcutContracts.Run();
            var defaults = new ShortcutSettings(null);
            for (int n = 0; n < 8; n++)
            {
                if (defaults[ShortcutSettings.Roles[n]].Text != ShortcutSettings.Defaults[n])
                    throw new Exception("Default changed");
                passed++;
            }

            var chosen = new Dictionary<string, string>
            {
                {
                    "letter",
                    "F8"
                },
                {
                    "word",
                    "Right Ctrl"
                },
                {
                    "unicode",
                    "Ctrl+Alt+K"
                }
            };
            var custom = new ShortcutSettings(null, chosen);
            if (custom.Mode(0x77, false, false, false, false) != "letter" || custom.Mode(0x4B, true, false, true, false) != "unicode" || custom.Mode(0x4B, true, true, true, false) != null)
                throw new Exception("Chord routing");
            passed++;
            if (custom.TapRole(0xA3, 1) != "word" || custom.TapRole(0xA2, 0) != null)
                throw new Exception("Ctrl tap routing");
            passed++;
            foreach (string value in new[]
            {
                "Ctrl+C",
                "Alt+F4",
                "F12",
                "A",
                "Ctrl+Ctrl+K",
                "Ctrl+Unknown"
            }

            )
                try
                {
                    new ShortcutBinding(value, true);
                    throw new Exception("Invalid accepted: " + value);
                }
                catch (InvalidDataException)
                {
                    passed++;
                }

            try
            {
                new ShortcutSettings(null, new Dictionary<string, string> { { "word", "`" } });
                throw new Exception("Duplicate accepted");
            }
            catch (InvalidDataException)
            {
                passed++;
            }

            string folder = Path.Combine(Path.GetDirectoryName(typeof(ShortcutSettingsTests).Assembly.Location), "KeyJaya-shortcuts-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "shortcuts.ini");
            new ShortcutSettings(file, chosen).Save();
            if (new ShortcutSettings(file)["word"].Text != "Right Ctrl")
                throw new Exception("Save lost");
            passed++;
            Console.WriteLine("PASS: " + passed + " FAIL: 0");
            return 0;
        }
    }
}
