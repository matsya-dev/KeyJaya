// Noninteractive regressions: per-span encoding decisions for a selected passage.
// Run: .\test.ps1 -Suite FontAutoSelectionTests. Fixtures live under out/; no resident hooks are started.
using System;
using System.Linq;
using System.Reflection;

namespace KeyJaya
{
    public static class FontAutoSelectionTests
    {
        public static int Main(string[] args)
        {
            int pass = 0;
            using (var core = new Core(args[0]))
            {
                var fonts = new FontEncoding(args[0]);
                foreach (string input in new[]
                {
                    "varna and Krsna",
                    "Tamala Krsna Goswami"
                }

                )
                {
                    int split = input.IndexOf(' ') + 1;
                    var snapshot = new InDesignSelection.Snapshot
                    {
                        Owner = new IntPtr(100),
                        Text = input,
                        Font = "Noto Serif",
                        Fonts = Enumerable.Range(0, input.Length).Select(n => n < split ? "Noto Serif\tRegular" : "ScaGoudy\tRegular").ToArray()
                    };
                    var edit = (SafeEdit)typeof(SafeEdit).GetMethod("InDesignEdit", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { snapshot, false, true });
                    ItalicSpan[] spans;
                    string result = edit.FontAutoOutput(core, fonts, out spans);
                    string expected = input.StartsWith("Tamala") ? "Tamāla " + EncodingMap.Balaram("Kṛṣṇa Goswami") : "varṇa and " + EncodingMap.Balaram("Kṛṣṇa");
                    if (result != expected)
                        throw new Exception("per-font text " + result);
                    pass++;
                    if (input.StartsWith("Tamala") ? spans.Length != 0 : spans.Length != 1 || spans[0].Start != 0 || spans[0].Length != 5)
                        throw new Exception("cross-font proper name/italic context");
                    pass++;
                }

                foreach (string mode in new[]
                {
                    "unicode",
                    "balaram",
                    "mixed"
                }

                )
                {
                    string input = "BG18.66VT";
                    var snapshot = new InDesignSelection.Snapshot
                    {
                        Owner = new IntPtr(100),
                        Text = input,
                        Font = mode == "balaram" ? "ScaGoudy" : "Noto Serif",
                        Fonts = Enumerable.Range(0, input.Length).Select(n => mode == "balaram" ? (n % 2 == 0 ? "ScaGoudy\tRegular" : "ScaTimes\tRegular") : (n % 2 == 0 ? "Noto Serif\tRegular" : "Calibri\tRegular")).ToArray()
                    };
                    if (mode == "mixed")
                        snapshot.Fonts = Enumerable.Range(0, input.Length).Select(n => n % 2 == 0 ? "Noto Serif\tRegular" : "ScaGoudy\tRegular").ToArray();
                    var edit = (SafeEdit)typeof(SafeEdit).GetMethod("InDesignEdit", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { snapshot, false, true });
                    ItalicSpan[] spans;
                    string result = edit.FontAutoOutput(core, fonts, out spans, true), expected = mode == "mixed" ? input : core.Trigger(input + (mode == "balaram" ? "<" : ">"));
                    if (result != expected || (mode == "mixed" ? spans != null : spans == null || !spans.Any(x => x.Force)))
                        throw new Exception("whole selected scripture across fonts " + mode);
                    pass++;
                }

                if (Core.ReencodeCurrent("rājā", "balaram") != "räjä" || Core.ReencodeCurrent("räjä", "unicode") != "rājā")
                    throw new Exception("encoding changed spelling");
                pass++;
            }

            Console.WriteLine("PASS: " + pass + " FAIL: 0");
            return 0;
        }
    }
}
