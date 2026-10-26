// Post-build smoke probe: load the compiled engine without invoking its resident entry point.
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using KeyJaya;

public static class RepositoryBuildProbe
{
    static int passed;
    static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        passed++;
    }

    public static int Main(string[] args)
    {
        string version = File.ReadAllText(Path.Combine(args[0], "VERSION")).Trim();
        Check(KeyJaya.Version.Current == version, "Packaged runtime version");
        Check(FileVersionInfo.GetVersionInfo(typeof(Core).Assembly.Location).FileVersion == version + ".0", "Packaged file version");
        using (var core = new Core(args[0]))
        {
            Check(!core.Short.Keys.Any(key => key.StartsWith("email", StringComparison.OrdinalIgnoreCase)), "No personal trigger in compiled app defaults or fallback");
            foreach (string input in new[]
            {
                "Krsna",
                "prasada",
                "Srila",
                "ācāryas",
                "Vyasa-puja"
            }

            )
                Check(core.CaretChoices(input, "unicode").Length > 0, "Core vocabulary retained: " + input);
            Check(core.CtrlShort("SP", "unicode") == "Śrīla Prabhupāda", "Short trigger retained");
            Check(core.CtrlShort("ISKCON", "unicode") == "International Society for Krishna Consciousness", "Exact expansion retained");
            Check(core.ConvertCopied("kRShNa", "unicode") == "kṛṣṇa", "ITRANS conversion retained");
            Check(core.ConvertCopied("kṛṣṇa", "devanagari") == "कृष्ण", "Devanagari retained");
            Check(core.ConvertCopied("kṛṣṇa", "bengali") == "কৃষ্ণ", "Bengali retained");
            Check(core.ScriptOptions.ShortcutMode("itrans") == "itrans", "Current tertiary default");
            Check(core.AutomaticCorrections.Spelling && core.AutomaticCorrections.Contractions, "Correction defaults enabled");
            Check(core.ConvertHighlighted("Earlier an age since raja and Krsna", "unicode").StartsWith("Earlier an age since "), "English protection retained");
        }

        string settings = Path.Combine(args[0], "config/settings.ini");
        Check(new StartupSettings(settings).Enabled, "Startup default retained");
        Check(new FormattingSettings(settings).Italicize, "Italics default retained");
        Check(new EncodingSettings(settings).Mode == "auto", "Encoding default retained");
        Console.WriteLine("PASS " + passed + " repository build checks against the compiled app");
        return 0;
    }
}
