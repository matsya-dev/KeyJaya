# Project layout

| Location | Contents |
| --- | --- |
| `src/` | Production C# source, manifests and required app configuration |
| `assets/` | Tray/app icon, factory trigger restoration and migration additions |
| `defaults/` | Fresh-install configuration; no installed preferences |
| `data/` | Runtime indexes, lookup resources and their license/provenance files |
| `tests/csharp/` | Isolated regression suites |
| `tests/fixtures/` | Negative English and conversion reference inputs |
| `third_party/` | Corresponding licensed dictionary sources |
| `tools/` | Repository audit and data-source maintenance helpers |
| `docs/` | User, developer, testing, publishing and licensing documentation |
| `out/` | Ignored application, installer and test outputs |

## Runtime components

- `Core`, `Vocabulary`, `CopiedConversion`, `Itrans`, `IndicScripts`, `Hindi`:
  recognition, transliteration, script output, spelling choices and data lookup.
- `SingleLetters`, `ResidentReplacement`, `SelectionCommit`, `SelectionFormatting`:
  target/cycle/replacement state and formatting.
- `Runtime`, `Native`: resident orchestration, Windows input and clipboard interfaces.
- `WordSelection`, `InDesignSelection`, `InDesignFindField`, `ExcelCellField`:
  editor-specific access and safe selection/commit handling.
- `Configuration`, `ShortcutSettings`, `TriggerEditorStore`: configuration,
  shortcuts, validated trigger editing and backups.
- `TrayMenu`, `TrayAppearance`, `WindowAppearance`, `CheatSheet`, `ShortcutWindow`,
  `TriggerEditorWindow`: tray/reference/editor UI and explicit monitor scaling.
- `Setup`, `SetupWindow`, `InstallTransaction`, `ApplicationShortcuts`,
  `EspansoMigration`, `Uninstall`: installation, migration, shortcuts and removal.

The resident is standalone. The Espanso migration class exists to preserve older
installations during upgrades; it is not a running dependency.

Installed user configuration, backups, logs and learned preferences are created
in the installed app's own directory. Building this repository does not touch it.
