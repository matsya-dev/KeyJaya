# Testing KeyJaya

From the repository root:

```powershell
.\test.ps1
```

The default set covers word/component targeting, slash boundaries, numbers,
single letters, contractions and race models, script handling, spelling/style
rules, shortcut configuration, trigger editing, native input structure construction
and feature toggles. Tests create private configurations beneath `out/tests/`.

Run one or several suites:

```powershell
.\test.ps1 -Suite ItransTests,HindiTests,TriggerEditorTests
```

Run all 44 included noninteractive suites:

```powershell
.\test.ps1 -Full
```

The full set includes large English-guard and dictionary/cycling checks and can
take several minutes and substantial temporary disk space. Test results are in
`out/tests/results.json`; individual logs are beside it. Fixtures are reset from
repository defaults before each suite. Input fixture copies are written under
`out/tests/inputs` so tests cannot overwrite committed test inputs.

These tests do not type into apps, read open documents, replace the clipboard,
register global shortcuts or start the resident. Native-key tests construct and
inspect event structures; they do not send those events. Interactive browser,
Word, InDesign, clipboard and live automation probes from development are excluded.

`CtrlShortTests` covers Ctrl scripture targeting, verse-only defaults, range
modifiers, paired translations, and abbreviation priority. `ScriptureRangeTests`
checks every adjacent pair in the bundled books in all three Roman encodings,
samples verse/prose italic boundaries, exercises continuous cycle undo origins,
and uses synthetic data for missing records and exact size limits. These check
conversion logic and formatting output; actual editor Ctrl+Z and Backspace
behavior still require the physical checks below.

The scripture suite also checks every record identifier against its stored index
hash, rejects duplicate identifiers, and verifies index order and record bounds.
The 276 repaired identifiers in `tests/fixtures/scripture-identifiers.tsv` must
resolve through both indexed lookup and Ctrl expansion, with SHA-256 hashes
matching their unchanged original verse/translation payloads.

## Physical release checks

Use separate disposable documents and a scratch browser editor. Check:

1. In Unicode and Balarama Word text: backtick letter cycles; Left Ctrl word,
   compound and selection conversion; punctuation and slash boundaries; undo.
2. Later-paragraph and first-document triggers, contractions and scripture insertion.
3. Pasted Word text while Paste Options is visible; rapid typing after contractions.
4. Track Changes, style/font preservation, mixed-font repair, English protection,
   correct italics and ordinary formatting after a converted word.
5. Selected/copied script conversions, clipboard preservation, Hindi/ITRANS choice.
6. Cheat sheet, trigger editor and shortcut editor on each monitor; light/dark/high
   contrast; repeated open/close and save/cancel.
7. Pause/startup options and ordinary Ctrl shortcuts.
8. InDesign text frames and Text/GREP Find/Change fields; Obsidian Live Preview;
   Excel cell editing; Acrobat comments; browser/ChatGPT/WhatsApp scratch inputs.
9. `BG2.13–14` + Left Ctrl inserts verses only; `BG2.13–14VT` pairs each verse
   with its translation. Both verses are italic; translations follow prose rules.
   Ctrl+Z should restore the full reference and original formatting in one step.
   Repeat with Track Changes and each encoding. Missing ranges must stay unchanged.
10. Cycle `Krishna` and `Rama` through multiple complete loops, then Backspace:
    restore the original spelling and formatting, preserving neighboring text.

Never use an existing working manuscript as a disposable fixture. Sleep/wake and
Windows sign-in tests are separate from the noninteractive suite.

## Continuous integration

The **Windows checks** GitHub Actions workflow runs on pushes to `main`, pull
requests, or a manual dispatch. Both PowerShell 7 and Windows PowerShell 5.1 jobs
use fresh Windows Server 2022 runners. Each audits the source and documentation,
checks bundled dictionary hashes, builds a verification installer, runs the
compiled-app probe and all included suites, then checks package integrity and isolated
installation/upgrade behavior.

Only verification logs/results are retained, for seven days. CI does not publish
installers, use signing secrets, register startup, or launch the resident. The
workflow has read-only repository access and uses pinned official actions.

These are fresh hosted build/test environments, not a test of physical typing
or installation on another person's desktop. Word, InDesign, Obsidian, browser
editors, sleep/wake, sign-in, and monitor interactions still need manual checks.
The shorter [first-run check](FIRST_RUN_CHECK.md) is suitable for initial testers.

For repeated local installer checks, use a fresh target instead of deleting an
existing fixture:

```powershell
python tools/verify_build.py --target out/verification/another-run/install
```

Targets must stay beneath `out/`. Existing target directories are refused.
