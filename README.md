# KeyJaya

KeyJaya is a Windows tray app for writing Sanskrit and Bengali. It adds diacritics,
expands abbreviations, converts scripts, and works with Unicode and Balarama fonts.

- Cycle a letter with backtick, or convert a word/selection by tapping Left Ctrl.
- Convert between IAST, Balarama, plain Roman text, ITRANS, Devanagari, and Bengali.
- Insert scripture passages and frequently used names and phrases.
- Correct common spelling mistakes and contractions.
- Apply eligible Sanskrit/Bengali italics while respecting names and style exceptions.
- Customize shortcuts and triggers through the tray menu.

The current version is **1.9.4**. Download the Windows installer from
[Releases](https://github.com/matsya-dev/KeyJaya/releases/latest), then see the
[user guide](docs/USER_GUIDE.md) for installation, shortcuts, and everyday use.

## Build and test

Requires Windows x64, .NET Framework 4.8, Windows PowerShell 5.1 or newer, and
Python 3.11 or newer. No NuGet packages or Python packages are required.

```powershell
.\build.ps1
.\test.ps1
```

The installer is written to `out/KeyJaya-Setup-v1.9.4.exe`. Building does not
install or launch the app. `out/` is ignored by Git.

- [Build instructions](docs/BUILDING.md)
- [Automated and manual testing](docs/TESTING.md)
- [Project layout](docs/ARCHITECTURE.md)
- [Preparing a GitHub release](docs/PUBLISHING.md)
- [Repository verification](docs/VERIFICATION.md)

## Repository scope and licensing

This is a clean source tree, not the development archive or an installed user
profile. Personal email expansions, preferences, logs, ebook inputs, old builds,
and live computer-automation probes are excluded. Default configuration is in
`defaults/`; the app's installed settings are separate.

Original KeyJaya code is available under the [MIT license](LICENSE). The runtime
data includes scripture/translation records that the project owner has confirmed
permission to distribute. Book content and third-party dictionaries retain their
own rights and are not relicensed under MIT; see
[licensing and data provenance](docs/LICENSING.md) and
[third-party notices](THIRD_PARTY_NOTICES.md).
