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

`TriggerEditorTests` checks that all resident editors and message boxes bypass conversion by
process ownership, keeping ordinary typing literal even when dialog titles change.
`ShortcutSettingsTests` exercises the Word shortcut reader through a getter-only
fake object model, including modifier mapping, the original navigation conflict,
normal/disabled/unassigned/prefix bindings, current proposed shortcuts, ownership
changes and release after a failed read. Disposable helper processes additionally
prove report decoding, malformed-output rejection and the five-second timeout.
Neither fixture controls Word or sends desktop input. These contracts remain
within the existing 44-suite registry.
The menu fixtures verify that the removed spelling editor and standalone Word
overlap actions stay absent while shortcut-window inspection remains available.

`BookStyleTests` also runs the Ctrl-italics contracts: unchanged spellings first,
plural forms, individual loanword overrides, explicit passage regularization,
proper names, whole compounds, English protection, distinct formatting cycle
positions, HTML and protected Markdown. `FontAutoSelectionTests` includes a
mixed-font passage whose unchanged terms require both italic and regular spans.
These fixtures exercise production policy and cycle code without typing into an
editor. Native undo and visible formatting still require user-led scratch checks.

The Word transaction-reuse fixture in `WordSnapshotTests` counts calls to a fake
range and fails on a duplicate text/revision read. It exercises production replay
capture and font restoration, including matching/stale font tickets, tracked
deletions, paragraph markers, provider failure, cancellation and one undo record.
It never connects to Word or sends desktop input.

`WordFormattingCycleTests` exercises actual native font capture/restoration on
fake Word ranges, including surrounding whitespace, punctuation, mixed fonts,
the selection bound and expiry while a Ctrl cycle remains active. It also checks
the production manual-undo decision and original-italic HTML payload. The fixture
never starts the resident or connects to Word. Ctrl-italics contracts additionally
cover literal Markdown identifiers, escaped/bounded emphasis, punctuation and
protected URLs, email addresses, paths and literal comments in all Roman modes.

Formatting and module-boundary guidance is in
[Contributing](../CONTRIBUTING.md) and [the code map](ARCHITECTURE.md).
The selection suite now includes the shared six-profile editor contract matrix.
`WordSnapshotTests` also exercises window-specific Word application binding,
including multiple instances, wrong window/process, stale global registration,
fallback and COM-reference ownership. Those tests use fake applications and do
not open or read native Word documents.
The same suite runs the resident's real font-ticket cleanup against a deliberately
stalled release backend on a separate STA caller. Navigation must return without
waiting for that backend, while the old ticket loses ownership immediately and
active leases release exactly once. `ReplayTests` also covers 256 Ctrl+Shift
navigation chords: both modifier sides/orders, arrows/Home/End/Page Up/Page Down,
autorepeat and both release orders. It checks FIFO order and extended-key flags
without sending input to Windows.
`ExcelCellTests` includes the real adapter's transaction methods against an
independent field backend. LF/CRLF selection, retention, punctuation, original
caret restoration, Enter/Edit transitions, stale reads, cancellation, ignored
selection and formula-output refusal are exercised without native input. The
one-pass discovery fixture checks that a failed capture stays ambiguous rather
than becoming a usable blank caret. These checks establish code contracts;
they do not establish every Excel provider's native cursor semantics.
The fixture also runs actual highlighted conversion across LF/CRLF passages,
models balanced Alt+Enter insertion, preserves nearby emoji/combining sequences,
and checks short relative travel inside long cells. Single-line Devanagari,
Bengali, ITRANS and plain-output regressions retain their established fallback.
Native retained selection and one-step undo were separately confirmed by the
user in a disposable Excel cell.
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

The same native test creates a real comment and threaded reply in its scratch
document. It checks their focus recognition, word and letter conversion,
abbreviation expansion, Unicode and ScaGoudy/Balarama output, cycling and original
font restoration, one-step undo, deferred undo/typing continuation and rejection
of expired or neighboring targets. Body text and the other comment/reply are
checked throughout. Draft and Print Layout fixtures explicitly activate their
owned comment editor; selecting a COM range in a hidden pane alone is not a
valid typing-focus fixture. UIA discovery is given the same bounded settling
window used by the resident. No keys are injected.

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

### Optional Modern Comments integration

```powershell
./tools/test_word_modern.ps1                # compile only
./tools/test_word_modern.ps1 -AllowDesktop
```

This separate opt-in fixture requires a current, hash-matching development
resident running with `--diagnostic`, Modern Comments enabled, and Word's English
Comments button label. It creates a new exclusively owned Word process and local
scratch document. It checks contextual and panel views, new comments and replies,
word/letter/abbreviation/selected conversion, one-step undo, unchanged body text
and ordinary English continuation with the editor's existing formatting.

Only its own verified comment editor receives fixture input. The resident's
normal Windows shortcut dispatch is used; injected-key filtering is not relaxed.
The diagnostic queue probe waits for selection retention and held input to settle
before assertions. This verifies the live editing path, not physical Ctrl-tap
recognition or hardware delivery timing. It does not open existing documents,
change the installed configuration, build an installer or publish anything.

### Physical editor checks

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
11. In a new Word comment and a reply, check letter/word cycles, abbreviation
    expansion, selected conversion, one-step undo and immediate English typing.
    Repeat with Unicode and ScaGoudy/Balarama in the comment pane and Print Layout.
    Verify the document body and neighboring comments/replies remain unchanged.

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
