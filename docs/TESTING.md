# Testing KeyJaya

## Developer-tool checks

The Python tooling has a separate standard-library test set:

```powershell
python -m unittest discover -s tests/python -v
```

These checks exercise archive exclusions and version guards, documentation links
and image headers, pinned dictionary-source verification, import safety, and
isolated-install verification logic. Network and installer calls are mocked;
fixtures live beneath ignored `out/python-tests` and are removed after each test.
The optimized-code check verifies that path guards remain active when assertions
are disabled. This set supplements the 44 C# suites and is included in CI.

Formatting and module-boundary guidance is in
[Contributing](../CONTRIBUTING.md) and [the code map](ARCHITECTURE.md).
The selection suite now includes the shared six-profile editor contract matrix.
`WordSnapshotTests` also exercises window-specific Word application binding,
including multiple instances, wrong window/process, stale global registration,
fallback and COM-reference ownership. Those tests use fake applications and do
not open or read native Word documents.
`StandaloneTests` checks the source-only distribution policy and reversible
retirement in disposable directories, including own-license retention, unchanged
user configuration and complete rollback. It locks each obsolete file in turn,
allowing backup reads while denying deletion, and verifies exact restoration of
configuration, old/new migration markers and retired files. Python checks enforce the same policy
at payload creation and reject excluded files in the staging tree.
See [Responsiveness verification](RESPONSIVENESS.md) for the automated 150 ms
engine benchmark and the separate native-editor smoke test protocol.

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

Each invocation uses a fresh fixture directory. Its editable trigger YAML is
copied directly from the authoritative factory list. `StandaloneTests` also
checks legacy/unified customization equivalence across all seven output modes,
override precedence, protected words, ordered replacements, invalid sections and
failed reloads. It verifies that loading leaves the original configuration bytes
unchanged. Shared conversion-plan tests cover abbreviations, verses, numbers,
names and continuous word cycles. These remain part of the 44-suite registry.
Conflicting unified replacement rules replace the matching legacy rules in their
first effective position; unrelated rules retain their order. `CtrlShortTests`
also verifies that an absent BB group stays absent on the first upgrade without
a migration marker, while fresh installs and explicit editor restoration retain BB.

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

## Setup rollback integration

```powershell
./tools/test_setup_transaction.ps1
```

This compiles the exact production setup sources as a **test library**, embeds a
source-data fixture and calls `Install` only with `test=true`. It does not build
or execute an installer, register startup, launch a resident or touch an installed
profile. Real locked-file failures cover early, middle and final retirements;
another failure occurs after all retirements. Every case checks the full original
file inventory and bytes, settings, trigger edits, migration markers, engine and
recovery backup. Fixtures and logs remain under `out/setup-transaction`.

## Optional native Word fault tests

```powershell
./tools/test_word_native.ps1                # compile only; Word is not started
./tools/test_word_native.ps1 -AllowDesktop  # requires Word and a quiet keyboard
./tools/test_word_native.ps1 -AllowDesktop -DelayMilliseconds 3000
# Physical typing: use the currently running resident and the NEW scratch doc.
./tools/test_word_native.ps1 -AllowDesktop -DelayMilliseconds 3000 -PhysicalTyping
```

This separate opt-in test creates a new Word application and scratch document,
then verifies that its process was absent before the run. It refuses a shared
process. It never enumerates or reads existing documents, starts KeyJaya hooks,
uses the clipboard or injects keyboard input. Only its own Word process is closed.

The test pauses only that new Word server's UI thread for 350 ms by default,
with a configurable upper bound of ten seconds and an independent resume thread.
It checks real native-object-model acquisition after
the delay, formatting and one-step undo, rollback after focus loss during an
edit, disconnected COM rejection, and the resident's queue recovery after the
actual Word window closes. Queue events are constructed and inspected for FIFO
ordering and paired modifier release; they are **not replayed into Windows**.
This supplements the fake-application binding tests rather than replacing them.

The optional physical mode waits up to ten minutes for a real Left Ctrl tap in
its own scratch document, then pauses that Word server while the person immediately
types ` next Word`. The existing running resident performs conversion; the helper
does not edit its settings. The test verifies exact suffix ordering, capitalization,
English formatting, released modifiers and undo, then closes only its own document.
It does not inject typing. Do not use another document for this check.

It does not prove behavior when a COM server never returns. Physical mode covers
real typing during a bounded stall only. Desktop Word is not installed on the normal CI runners, so this test
is deliberately excluded from the noninteractive 44-suite registry and CI.

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
installation/upgrade and post-retirement rollback behavior. Both jobs also run the setup-retirement rollback
integration test; its logs are retained with the other verification artifacts.

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
