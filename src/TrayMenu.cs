// KeyJaya: Declarative tray labels, stable command IDs, and checked/enabled states.
// Resident dispatch uses these IDs; changing their meaning requires updating both sides.
using System;

namespace KeyJaya
{
    public sealed class TrayItem
    {
        public readonly uint Id, Flags;
        public readonly string Label;
        public TrayItem(uint id, string label, uint flags = 0)
        {
            Id = id;
            Label = label;
            Flags = flags;
        }
    }

    public static class TrayMenu
    {
        static TrayItem Separator()
        {
            return new TrayItem(0, null, 0x800);
        }

        public static TrayItem[] Items(bool paused, bool spelling, bool contractions, string encoding = "auto", bool precise = false, bool preserveVerseMarkers = false, bool italicize = true, string language = "sanskrit", bool startup = true, bool manageStartup = true)
        {
            return new[]
            {
                new TrayItem(21, "Cheat sheet"),
                Separator(),
                new TrayItem(19, "Autocorrect spelling", spelling ? 8u : 0u),
                new TrayItem(20, "Automatic contractions", contractions ? 8u : 0u),
                new TrayItem(30, "Auto-italicize conversions", italicize ? 8u : 0u),
                Separator(),
                new TrayItem(22, "Encoding: Auto (detect font)", encoding == "auto" ? 8u : 0u),
                new TrayItem(23, "Encoding: Unicode / IAST", encoding == "unicode" ? 8u : 0u),
                new TrayItem(24, "Encoding: Balarama / Legacy", encoding == "balaram" ? 8u : 0u),
                Separator(),
                new TrayItem(31, "Ctrl+\\: ITRANS", language == "sanskrit" ? 8u : 0u),
                new TrayItem(32, "Ctrl+\\: Hindi", language == "hindi" ? 8u : 0u),
                new TrayItem(25, "Precise script conversion", precise ? 8u : 0u),
                new TrayItem(26, "Keep verse markers and numbers", preserveVerseMarkers ? 8u : 0u),
                Separator(),
                new TrayItem(34, "Customize shortcuts…"),
                new TrayItem(35, "Edit triggers…"),
                new TrayItem(5, "Open configuration folder"),
                new TrayItem(17, "Reload configuration"),
                Separator(),
                new TrayItem(33, "Start with Windows", (startup ? 8u : 0u) | (manageStartup ? 0u : 2u)),
                new TrayItem(1, paused ? "Resume KeyJaya" : "Pause KeyJaya"),
                new TrayItem(7, "Restart KeyJaya"),
                new TrayItem(8, "Exit")
            };
        }
    }
}
