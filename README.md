<p align="center">
  <img src="docs/images/keyjaya-logo.png" alt="KeyJaya की logo" width="96" height="96" align="middle">
  &nbsp;&nbsp;
  <img src="docs/images/keyjaya-demo.gif" alt="Typing and converting Hare Krishna, cycling Rama spellings, and expanding SP ki jaya" width="254" height="96" align="middle">
</p>

# KeyJaya

[![Windows checks](https://github.com/matsya-dev/KeyJaya/actions/workflows/ci.yml/badge.svg)](https://github.com/matsya-dev/KeyJaya/actions/workflows/ci.yml)

KeyJaya is a Windows tray app for writing Sanskrit and Bengali. It adds diacritics,
expands abbreviations, converts scripts, and works with Unicode and Balarama fonts.

## Download and get started

**[Download KeyJaya for Windows](https://github.com/matsya-dev/KeyJaya/releases/latest)**

1. Open the release and download **KeyJaya-Setup-v1.9.4.exe** under **Assets**.
2. Run setup, then find the white **की** icon in the system tray, possibly under
   the hidden-icons arrow. Open **Cheat sheet** from its menu.
3. In scratch text, type `Krsna` and tap **Left Ctrl**. It becomes `Kṛṣṇa` in a
   Unicode font, or the equivalent legacy text in a Balarama font.

Requires **Windows x64 and .NET Framework 4.8**. Balarama fonts are supplied
separately. The installer includes the app and its dictionaries; Python, Git,
and developer tools are only needed to build from source.

The current installer is unsigned, and Windows may display an unrecognized-app
warning. Download from this repository's Releases page and compare its SHA-256
with **SHA256SUMS.txt**. Keep Windows security protection enabled.

## Everyday examples

| What you do | Unicode result |
| --- | --- |
| Type `a`, then press backtick | `ā`; another press returns to `a` |
| Type `raja`, then tap Left Ctrl | `rāja`; more taps cycle through spellings and plain text |
| Type `SP`, then tap Left Ctrl | `Śrīla Prabhupāda` |
| Select `kṛṣṇa`, then press Ctrl+[ | `कृष्ण` |
| Select `kṛṣṇa`, then press Ctrl+] | `কৃষ্ণ` |
| Type `doesnt` followed by a space | `doesn’t` |
| Type `BG18.66>` | Inserts the Bhagavad-gītā 18.66 verse |

Normal Ctrl+C and Ctrl+V continue to work. KeyJaya's conversion shortcuts take
priority over matching app commands while it is active. Use **Customize shortcuts**
to change them, or **Pause KeyJaya** to restore the app's normal key behavior.
**Start with Windows**, contractions, spelling correction, and automatic italics
can each be controlled in the tray menu.

![KeyJaya tray menu with default settings](docs/images/tray-menu.png)

*The app's menu rendered with default settings at 100% scale in the light theme.
It also follows Windows dark/high-contrast settings and monitor scaling.*

## Main features

- Cycle a letter with backtick, or convert a word/selection by tapping Left Ctrl.
- Convert between IAST, Balarama, plain Roman text, ITRANS, Devanagari, and Bengali.
- Insert scripture passages and frequently used names and phrases.
- Correct common spelling mistakes and contractions.
- Apply eligible Sanskrit/Bengali italics while respecting names and style exceptions.
- Customize shortcuts and triggers through the tray menu.

The current version is **1.9.4**. The [user guide](docs/USER_GUIDE.md) explains all
eight default actions, encoding choices, editing triggers, undo, and everyday use.

## App compatibility and feedback

These editing areas have been exercised during development:

| App | Editing areas |
| --- | --- |
| Microsoft Word | Body text, footnotes/endnotes, Unicode/Balarama fonts, Track Changes, pasted text |
| Adobe InDesign | Text frames, footnotes/endnotes, Text/GREP Find/Change fields |
| Zen/browser editors, ChatGPT, WhatsApp | Editable plain-text and rich-text fields |
| Obsidian | Writable Markdown, including Live Preview |
| Microsoft Excel | A cell in edit mode with a visible text caret |
| Adobe Acrobat | Sticky-note comments, replies, and text-box comments |

App versions and controls can behave differently. Plain-text fields cannot store
italics. Script conversion accepts phonetic input, but ambiguous Roman spellings
can have multiple readings. Review converted text before relying on it in a
finished document. An independent installation on another user's computer is
still awaiting validation; hosted CI checks do not exercise these editors.

Start with the [five-minute check](docs/FIRST_RUN_CHECK.md) in a disposable document.
If something fails, [report a bug](https://github.com/matsya-dev/KeyJaya/issues/new?template=bug-report.yml)
with the KeyJaya/app versions, font, encoding setting, exact scratch text, and keys
pressed. Use the [private security reporting process](SECURITY.md) for vulnerabilities.

Read [Privacy and local data](docs/PRIVACY.md) for keyboard monitoring, clipboard
handling, saved preferences, and diagnostic logs.

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
