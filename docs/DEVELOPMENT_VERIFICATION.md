# Ctrl scripture insertion, ranges, and cleaner cycles

This is the development-stage record. The completed 1.9.5 release checks are
recorded in [Release verification](VERIFICATION.md).

Verified locally on 23 October 2026 on `codex/ctrl-verse-insertion`.
This section records logic-only feature verification before the subsequent
[Excel investigation](EXCEL_DEBUGGING.md). It is not a packaged release. No
installer was built. The Excel report separately records its isolated resident
and disposable-workbook tests.

## Automated results

- **43 isolated suites passed; 25,161,990 assertions; zero failures.** Results
  combine the full regression run with the new range suite and targeted reruns
  after the ACBSP and SSR updates. Logs are in `out/tests/combined-feature-verification.json`.
- Checked **36,684 adjacent verse ranges across BG, SB, CC, CB, ISO, BS, and NOI**
  in Unicode, Balarama, and plain Roman output.
- Checked 1,131 distributed formatting cases, including both forced verse italics
  and prose translation boundaries. Word span merging, rich-editor HTML, and
  Markdown output preserve the text and verse/translation order.
- Checked 4,011 shared-translation cases across the encodings. Adjacent verses
  sharing a translation include it once; nonadjacent repetitions stay in place.
- Bare references and ranges remain verse-only; `T`, `VT`, and `R` retain their
  explicit meanings. Ctrl, selected-reference conversion, and symbol endings
  produce consistent content. Punctuation, surrounding text, and abbreviation
  priority are covered.
- Synthetic data verifies missing first/middle/last records, translation-only
  records, the exact 200-verse limit, and the exact 200,000-character output limit.
  Missing or oversized passages return no partial insertion.
- Cleaner cycles retain validated alternatives and the original plain spelling,
  remove tested partial/case duplicates, and loop continuously. The original undo
  spelling survives 100 complete loops, including through the plain option.
  English protection and preferred-spelling tests also pass.
- ACBSP defaults now use `A.C. Bhaktivedanta Swami Prabhupāda`, with corresponding
  Balarama and plain forms. The upgrade correction preserves custom and exact
  replacements, disabled/deleted variants, comments, encoding, and unrelated YAML.
- All 30 runtime-data checksums and local documentation links were checked.
  Only the requested short-trigger TSV changed in the runtime data pack.
- SSR expands to `The Science of Self-Realization` with Ctrl or `>`, `<`, and `?`.
  Its independent upgrade marker allows installation after the earlier SBB
  addition, preserving custom/disabled SSR entries and subsequent deletions.

## Remaining release checks

These tests exercise conversion logic, caret targeting, formatting output,
ownership checks, and undo state. They do not prove physical input timing or an
editor's native undo stack. Before packaging, use disposable Word documents to
check Ctrl+Z after range insertion, Backspace after cleaned word cycles, original
font/style restoration, Track Changes, and queued typing. The examples are in
[Testing KeyJaya](TESTING.md#physical-release-checks).

The accepted source baseline and existing release verification remain for v1.9.4;
they have not been refreshed to label these unbuilt changes as a release.

## Consolidation — 24 October 2026

Unreleased work on `codex/consolidate-definitions`, based on the published 1.9.5
source. The accepted 1.9.5 baseline, installed app and existing installer are
unchanged. No installer was built, no resident was activated, and no live editor
or open document was controlled during this pass.

- `assets/short-trigger-defaults.yml` is the sole maintained trigger definition.
  Fresh editable YAML is copied from it; restoration, requested upgrade groups
  and fallback lookup use it too. Its original bytes are unchanged. Separate
  default, upgrade-addition and TSV fallback lists were removed. This also makes
  `BB` available in fresh defaults. Existing edited/deleted/disabled canonical
  triggers remain authoritative. Legacy personal-YAML fallback behavior is tested.
- Seven comment-only customization templates were replaced by one sectioned
  `customizations.ini`. It covers aliases, preferred/protected words, script words,
  script phrases, italics and ordered replacements. Legacy files are read with
  their existing precedence, without rewriting them. Unified entries override
  legacy entries of the same kind; word preferences still win over general aliases.
- Caret and selection commands share word preparation and cycle choice; copied
  and selected commands share source-encoding dispatch. Editor-specific selection,
  native replacement, clipboard ownership, fonts, undo and input queues remain
  separate. Large dictionary/index files were not consolidated.
- Future builds use a fresh output tree, retaining prior development output.
  Future upgrades archive the two obsolete factory-only files in the transaction
  backup. User customization files are retained. An isolated transaction probe
  verified cleanup, settings preservation and complete rollback.

Verification: all **44 suites passed, 26,936,540 assertions, zero failures**.
After a final parser fix for literal bracketed replacement rules, the affected
`StandaloneTests` rerun passed **41,722 assertions**; the latest result for each
of the 44 suites totals **26,936,542 assertions**. These additions test all seven
output modes, legacy/unified equivalence, precedence, invalid/duplicate sections,
failed reloads, protected words, exact triggers, continuous cycles and no writes
while loading configuration.

A separate probe compiled both the released and refactored engines and compared
**35,088 behavior records** with identical results: word/copied/selected conversion,
spelling choices, formatting decisions and verse expansion. Preservation checks
confirmed all **27 installed/temporary configuration files** and all **29 retained
runtime-data files** unchanged. Only the redundant trigger TSV was removed from
the data inventory. Development audit, documentation links, script syntax and
setup-source library compilation passed.

Detailed local evidence is in `out/consolidation/verification.json`,
`full44-results.json`, `final-customization-rerun.json`, `combined44-results.json`,
`differential-verification.json` and `preservation.json`. The strict release audit
still detects intentional source drift; `--development` reports it without
refreshing the accepted baseline. This pass verifies automated logic and
configuration compatibility, not new physical editor timing or native undo tests.

### BB upgrade follow-up

The automatic BB repair described below was superseded by the 26 October review
fixes at the end of this report. An absent BB group is no longer restored during
upgrade without an explicit user action.

The installed editable trigger file lacked `BB`, although the factory list had
it. The upgrade-addition groups included SBB and SSR but omitted BB. BB now has
its own one-time addition marker, with tests for missing, custom, disabled,
partial and empty configurations, idempotency and subsequent user deletion.
`CtrlShortTests` passed 5,231 assertions after this change.

The four missing BB forms were also appended to the installed configuration,
backing up and preserving every original YAML byte. All 14 other configuration
files stayed unchanged. Loading the actual installed engine in a nonresident
probe verified Ctrl and explicit-symbol outputs in all three Roman encodings.
The running resident needs Reload configuration to see the added entries; no
installer or replacement resident was built. Evidence is in
`out/bb-trigger/verification.json`.

## Code-quality pass — 24 October 2026

This pass starts from the unreleased consolidation and BB repair above, rather
than reverting to the published source. It changes organization, formatting and
documentation while preserving executable C# bodies. The accepted 1.9.5 baseline
and existing installer remain unchanged.

- Formatted 85 C# files with a C# 5 parser and executable-token comparison. Added
  purpose comments to modules and suites, and ownership/ordering contracts at
  edit, queue, configuration and indexed-data boundaries.
- Moved existing types into `Program.cs`, `SafeEdit.cs` and `IndexedStore.cs`;
  updated explicit compiler input lists. All 146 type groups retain identical
  executable tokens, literals, member order and partial-declaration contents.
- Made Python tools import-safe, separated their functions from command-line
  entry points, and kept integrity/path guards active under optimized Python.
  Optional image dependencies load only when rendering is requested. PowerShell
  scripts now document their purpose and expose readable compiler argument lists.
- Added formatting conventions, pinned optional developer tools, a contributor
  guide and a code map. The new standard-library Python checks are wired into CI.
  Factory configuration changes are comments only; parsed rows are unchanged.

| Verification | Result |
| --- | --- |
| Complete C# registry | 44 suites; 26,936,571 assertions; zero failures |
| Python developer-tool boundaries | 20 tests passed; network/installer mocked |
| Pre-pass/current conversion comparison | 35,088 behavior records identical |
| PowerShell compatibility | All 8 scripts parse in 5.1 and 7; focused suite runs in 5.1 |
| Single-suite results | JSON remains an array; full 44-suite evidence retained |
| Setup/uninstaller source | Both compile as libraries; neither entry point executed |
| Documentation renderer | Old/new GIF, PNGs and timeline byte-identical in disposable folders |
| Preservation | 52 installed-config, data, image and existing-installer hashes unchanged |
| Repository checks | Development audit, local links, image headers and pinned Hindi sources pass |

Detailed local evidence is retained under `out/code-quality`, including the
pre-pass source snapshot, `verification.json`, `full44-results.json`,
`csharp-body-proof.json`, `differential-verification.json`, `preservation.json`,
`python-results.txt` and `render-parity.json`.

No installer was built, resident replaced, installed setting changed, or native
editor/document controlled. These checks establish source and automated behavior
preservation; they do not measure fresh physical typing latency or native editor
integration. The strict release audit intentionally still rejects source drift;
development mode reports it without refreshing the accepted baseline.

## Responsiveness and shared contracts — 25 October 2026

Implemented the follow-up recommendations without replacing the installed
resident or building an installer.

- Added `tools/benchmark.ps1`, a noninteractive runner, and a checked-in 150 ms
  warm engine p95 budget. Seventeen cases cover the principal conversion paths.
  Startup and first requests are reported separately from warm percentiles. CI
  runs the gate and retains JSON reports.
- Added one shared nine-scenario matrix for six editor-capability profiles to
  the existing `SelectionTests` suite. Both Unicode and Balarama are covered,
  including target boundaries, English/numbers, ignored and delayed selection,
  focus loss, lost selections, continuous cycles and original-spelling retention.
  Production Word revision mapping, InDesign GREP protection, Excel formula
  exclusion and browser paragraph normalization also run in that shared check.
  These are deterministic models, not native editor automation.
- Extracted 14 input lifecycle and replay-acknowledgment methods into
  `src/ResidentInput.cs`. This is a partial of the existing resident, with no new
  state owner, worker, timer or hook. All 152 resident member token sequences
  match the pre-pass snapshot. The 72 field declarations remain in their original
  order in `Runtime.cs`; other production-source files are unchanged.

Verification: all **44 C# suites passed, 26,940,440 assertions, zero failures**.
The shared matrix contributes **3,869 assertions**. The 20 Python developer-tool
tests also pass. A separate before/after comparison produced **35,088 identical
behavior records**. Setup sources compile as a library without executing setup.

The full benchmark measured **301 warm samples per case**, with no budget misses.
The largest observed p95 was **7.5263 ms**, for the longer selected passage.
Engine initialization was **357.6626 ms**, reported separately. This does not
measure complete native-editor latency. A deliberately impossible budget was
rejected with the expected nonzero status; a PowerShell 5.1 benchmark run also
passed. Nine PowerShell scripts parse in both 5.1 and 7, and all 88 C# files pass
the formatter check.

All **52 preservation hashes** and the factory asset/default bytes remain
unchanged. The accepted source baseline is not refreshed. Development audit,
documentation links and image checks pass. No open document was controlled.

Evidence is retained under `out/responsiveness`: `verification.json`,
`full44-results.json`, `benchmark-results.json`, `negative-budget-results.json`,
`queue-proof.json`, `differential-verification.json`, `preservation.json`,
`python-results.txt`, and the pre-pass source snapshot. The separate native smoke
test protocol and measurement limits are in
[Responsiveness verification](RESPONSIVENESS.md).

## Word instance binding fix — 25 October 2026

Ctrl word/short-trigger/scripture conversion and explicit `>` / `<` / `?`
triggers could briefly select their target in Word without replacing it. The
installed resident used a global Word application lookup for all six automation
paths. With multiple Word processes running, that lookup failed to expose the
active document window while the actual editor's native window interface worked.

Added `WordApplication.cs` for window-specific binding through Word's native
object model. The helper verifies the application window and process, uses global
registration only as a verified compatibility fallback, and transfers/releases
each acquired COM reference explicitly. Selection, document identity, expected
text, formatting, replay and undo checks remain in `WordSelection`. Font lookup,
replay anchors, caret anchors and replacement all use the same resolver.

Thirteen targeted suites passed **3,227,798 assertions**, including **182 new
binding assertions**. The new cases cover competing instances, wrong window or
process, missing native interfaces, disconnected objects, safe fallback and
reference ownership. Live read-only metadata probes verified the correct Word
window from both STA and MTA threads. A background window was correctly rejected.
The probes did not read document text, selection or document names.

A temporary patched executable was activated using the existing installed
configuration. The installed executable and all **52 configuration/data/asset/
installer preservation hashes** remain unchanged. Neither Word process was
closed, and no document was controlled through automation. Physical typing
confirmation was subsequently received: `K`, `SP`, `prasada` and `BG1.1` with
Left Ctrl, plus `prasada>`, `SP<` and `SP?`, all converted without leaving their
targets highlighted. No installer was built.

Evidence is retained under `out/word-instance-fix`: `verification.json`,
`combined-results.json`, `ownership-before.json`, `ownership-fixed-active.json`,
`preservation.json`, `activation.json`, and the pre-fix source snapshot.

## Final pre-installer checks — 25 October 2026

The complete 44-suite registry was rerun on the Word-fixed source and passed.
After the requested distribution change, the affected `StandaloneTests` suite
was rerun with 51 new policy/transaction assertions and passed 41,773 assertions.
The latest result for each suite totals **26,940,673 assertions, zero failures**.
All **23 Python tooling tests** also pass.

The engine benchmark passed all **17 cases at 301 warm samples each**, with a
largest observed p95 of **5.0234 ms** against the 150 ms budget. These are engine
measurements, not complete native-editor timings. The user confirmed live
conversion, cycling, Backspace undo and continued typing in scratch text in
**Balarama Word, a browser, InDesign and Excel cell editing**.

The owner explicitly confirmed separate permissions to omit the third-party
notices from the application. KeyJaya's original copyright/MIT notice remains
byte-identical. Original upstream records remain in development sources.
`assets/distribution-exclusions.txt` is shared by staging, ZIP packaging and
backed-up upgrade retirement. Its validators reject user/configuration paths,
runtime dictionaries, traversal, duplicate entries and Windows filename aliases.
Rollback tests restore every original fixture file.

The staged app and review payload contain **38 files**, including all **19
runtime data files** with source-matching hashes; **12 source-only files** are
omitted. The compiled app passed its 19-check nonresident probe. Setup compiles
from its declared 19 sources as a library, without producing or executing an
installer. A separate snapshot exercise of actual configuration migration
preserved all **15 user configuration files**, including the canonical trigger
YAML, byte for byte. All **52 protected input hashes** remain unchanged.

Review also found broken relative links in the installed user guide. The staged
app now includes the first-run, privacy and security help plus the existing tray
image, with all six local help links valid. Repository documentation is unchanged
by this relocation. All 92 C# files pass formatting checks; nine PowerShell
scripts parse in both 5.1 and 7.

Evidence is in `out/pre-release-check-20261025`: `verification.json`,
`full44-results.json`, `latest44-results.json`, `distribution-csharp-results.json`,
`python-results.txt`, `benchmark-results.json`, `package-review.json` and
`profile-preservation.json`. The review ZIP is an internal payload check, not an
installer. The installed executable, live configuration, working documents and
accepted release baseline were not changed, and nothing was published to GitHub.

## Code-review fixes — 26 October 2026

Both P2 findings were reproduced with failing regressions before the fixes. No
finding was rejected. Installer rebuilding and GitHub publication remain held.
The installer produced before this review is superseded and is not evidence for
the revised source.

- Removed BB from automatic upgrade additions. An absent group, including an
  intentionally empty YAML file without `bb-trigger-added.flag`, is left absent.
  Existing custom, disabled and partial groups remain byte-identical. Fresh
  defaults and an explicit trigger-editor restoration still provide all four BB
  forms. The established SBB/SSR addition behavior is unchanged.
- Unified `[replacements]` entries now override identical case-sensitive legacy
  FROM keys in their first legacy position, removing stale duplicate rules. New
  keys append afterward; unrelated rules retain their order. Tests cover the
  reported conflict, cascades/order, empty output, case sensitivity, copied
  conversion and byte-identical input files.
- Added real locked-file failures at every distribution retirement position to
  `StandaloneTests`. Backup reads succeed while deletion fails. Rollback restores
  the exact configuration, existing and newly introduced migration markers, and
  retired files; the recovery backup remains complete.
- Added a setup integration test that compiles the exact production setup
  sources as a test library and invokes `Install(test=true)`. Early, middle and
  final retirement failures, plus failure after all retirements, restore the
  complete original installation inventory. This produced no installer and did
  not register startup, launch a resident or use an installed profile. Both CI
  shell jobs now include this check.
- Added a separate opt-in native Word test, excluded from CI and the 44-suite
  registry. It verifies a new, exclusively owned Word process and scratch
  document before fault injection. It exercises delayed native COM binding,
  actual italic/font preservation, one-step native undo, focus-loss rollback
  after a real patch, disconnected COM cleanup and queue recovery after the
  actual Word window closes. Existing documents are not read or edited.

Verification: the full **44 suites passed, 26,940,779 assertions**, then the final
focused `CtrlShortTests` rerun added five explicit editor-restoration checks.
The latest result per suite totals **26,940,784 assertions; zero failures**.
All **23 Python tooling tests**, **25 setup integration checks** and **32 native
Word checks** passed. All **17 engine benchmarks**, with 301 warm samples each,
passed the 150 ms budget; the largest p95 was **9.348 ms**. Benchmarks measure the
engine, not complete editor latency. All 94 C# files pass formatting checks.

Native coverage is bounded: the Word server delay is 350 ms and uses an independent
resume thread. The elapsed lookup measurement excludes watchdog cleanup. Queued
events are constructed and inspected for FIFO order and paired modifier releases,
not injected into Windows. A permanently hung COM server and end-to-end physical
keystroke replay during a hang remain outside these automated checks. Earlier
physical-key results do not establish those missing cases.

Reproduction commands and safeguards are in [Testing](TESTING.md). Local evidence
is under `out/review-20261026`, `out/setup-transaction` and `out/native-word`.

## Word comments and replies — 26 October 2026

Development from released 1.9.6. The original native reproduction could read and
select `prasada` in a comment, but focus recognition returned false and native
replacement was rejected. The comments story (4) was excluded; its writable
`_WwG` Edit provider also had neither a Body ID nor a Document ancestor.

The native Word path now accepts comments and threaded replies while retaining
its existing window/process, document, exact-text, formatting, undo and replay
checks. The additional focus path requires the foreground Word process, the
native `_WwG` editor and a writable TextPattern selection. It does not admit
ribbon/search controls, menus or arbitrary Edit fields. No configuration loading,
trigger defaults, queue ownership or other editor adapter was changed.

Verification: all **44 isolated suites passed, 26,940,793 assertions; zero
failures**. The comments-story regression failed before the fix. All **26 Python
tooling tests**, **19 compiled-app checks** and **17 engine benchmarks** passed.
The highest warm engine p95 was **11.207 ms**, within the 150 ms budget; these
measure the engine rather than complete editor latency. Development audit,
pinned Hindi sources, documentation links and formatting checks passed.

The expanded native Word fixture passed **213 checks**. It creates a real comment
and threaded reply in its exclusively owned scratch document and checks Draft
and Print Layout comment panes, Unicode and ScaGoudy/Balarama, word/letter edits,
abbreviation expansion, cycling and original fonts, native undo, continued typing,
expired context and neighboring-target rejection. Body text and the other
comment/reply remain unchanged. The fixture must display and activate its own
comment pane after a view change; a COM selection in a hidden pane does not prove
typing focus. A separate UI-focused Print Layout reproduction also converted.
The existing bounded COM-delay, focus-loss rollback and disconnection checks pass.
Native tests inject no keys. Modern Comments and physical end-to-end hotkey timing
are not newly verified by this pass.

An application-only development executable was activated with the existing
installed configuration after a keyboard-idle check and graceful resident close.
All **41 protected installed configuration/data/asset/executable hashes** remained
unchanged, as did Windows startup registration. The temporary build's reference
window reads the same live configuration through a junction beneath ignored
`out/word-comments`; dictionary/asset copies remain under that temporary root.
Existing working documents were not read, edited or closed. The installed
executable, version and accepted 1.9.6 baseline are unchanged. Windows startup
still launches the released executable. No installer was built or published.

Evidence is under `out/word-comments`: `reproduction.json`,
`print-conversion.json`, `full44-results.json`, `native-check-6.txt`,
`native-check-6/editing-trace.txt`, `activation.json` and `verification.json`.

## Modern Comments and conversion delay — 26 October 2026

Modern Word comments use UIA card editors rather than the document's COM
Selection. Their focus event identifies the real editor even when the global
focused-element query returns a stale body provider. The adapter checks live
focus, Word process, card ancestry and the exact owned native host (`_WwG` for
contextual bubbles; `NetUIHWND` for the Comments panel). Modern text replacements
retain the draft's existing formatting, and body COM/font operations are excluded.

The user confirmed that conversion now works in the comment fields, then reported
excessive delay. Earlier native contextual new-comment/reply cases completed;
the full actual-panel reply fixture was interrupted and is not recorded as a
complete native pass. This responsiveness pass did not control the desktop or
open any Word document.

Existing diagnostic samples showed approximately 1.18–1.26 seconds from an
accepted word shortcut to held-input release. Focus resolution queried the stale
global provider first, fetched individual ancestry properties repeatedly and
walked the same comment tree twice. Word shortcuts also repeated native-body
Paste Options preparation in the modern comment editor.

The development path now resolves the live event editor first, fetches properties
in a per-call UIA CacheRequest and returns the successful classification to the
document-focus guard. Modern word shortcuts use their own caret/selection proof
directly, removing duplicate selection/menu probes and two fixed 40 ms waits.
Current focus, process, ancestry, native ownership, exact text/ranges, modifier
release and input-generation checks remain. No ownership or text proof is cached
between calls.

All **12 targeted isolated suites passed, 1,583,043 assertions; zero failures**.
WordSnapshotTests includes **103 new provider-model assertions** (593 total in
that suite) covering zero global-focus fallback calls for repeated valid comment
resolutions, fresh ancestry on every call, stale focus, changed/mixed processes,
normal body fallback, missing providers, cycles and the 18-node bound. Selection,
replay, correction races, native-key contracts, resident replacement, shortcut
settings, Word ranges/font modes and abbreviation checks passed. All **19
compiled-app checks**, development audit, documentation-link and C# formatting
checks passed. The optional modern native fixture compiled without running.

These checks establish the removed work and retained safeguards; they do not
measure a new live Word end-to-end latency. Release 1.9.6 and its accepted baseline
are unchanged. No installer was built and nothing was published. Evidence for
this pass is `out/modern-word-comments/speed-targeted-results.json`.

The optimized application-only build is running from the temporary
`out/word-comments/resident-comments-speed-1` folder with the existing installed
configuration. Activation verified the expected prior resident and its hidden
window/process, modifier release and queue acknowledgement, then requested a
graceful close and confirmed the new resident window. Active Word editing blocks
the restart. All **41 protected installed configuration/data/asset/executable
hashes** and Windows startup registration were unchanged. The installed/startup
executable remains released 1.9.6; this development activation is temporary.
The activation record is `out/modern-word-comments/activation.json`.

## Word selection shortcut and offline Excel review — 26 October 2026

The reported Ctrl+Shift+Left problem had a verified Word assignment conflict.
A read-only query against the application bound to the visible Word frame found
Ctrl+Shift+Left assigned to `RejectChangesAndAdvance`, Ctrl+Shift+Right to
`AcceptChangesAndAdvance`, and Ctrl+Left to `RejectChangesSelected`. The modern
comment editor handles navigation separately, explaining the different behavior.
The user removed the conflicting assignments. A subsequent read-only query
confirmed `WordLeftExtend`, `WordRightExtend`, `WordLeft` and unchanged
Shift+Left `CharLeftExtend`. The agent did not modify or save Word shortcuts,
templates or documents. The template still reported unsaved changes at that
check; their persistence remains under Word's normal save lifecycle.

Code review separately found native Word font/document-reference release in the
low-level keyboard hook when navigation clears the previous conversion ticket.
A deliberately stalled release backend reproduced that blocking path in the
resident's actual cleanup method. Cleanup now retires the ticket synchronously
and releases its detached references on a worker. Active font leases retain
their references until their existing user releases them; retirement cannot
authorize another edit. Eight regression assertions cover nonblocking return,
immediate invalidation, repeated cleanup and active-lease lifetime. This is a
separate responsiveness correction, not the cause of the Word key assignment.

Excel mirror identity/pattern properties now use a fresh batched UIA request.
Collapsed carets prove endpoint equality and read their prefix once; selected
ranges retain both offset and exact-text checks. Read-plan tests fail if a
collapsed caret requests the unnecessary end-prefix/selected-text reads. Formula
exclusion, provider deadlines, single-request ownership, epoch/current editor and
Enter/Edit checks remain. Hidden Formula Bars, multiline cells, localized mode
labels and larger buffers are documented separately in [Excel findings](EXCEL_DEBUGGING.md).
They were investigated but were not enabled without native selection evidence.
Comment formatting/italics were deliberately left as requested by the user.

All **12 targeted isolated suites passed, 1,584,902 assertions; zero failures**.
This includes WordSnapshotTests (601), ExcelCellTests (2,220), ReplayTests
(47,890), and the existing selection, correction-race, native-key, letter,
replacement, shortcut, Word range and font-mode checks. The expanded replay
coverage includes **256 Ctrl+Shift navigation chords**, both modifier sides and
press/release orders, autorepeat, FIFO delivery and extended-key flags. The six
editor-capability profiles and nine shared selection scenarios passed again.
These are model/production-method checks, not new native editor interaction.
All **19 compiled-app checks**, **98 C# formatting checks**, development audit
(1,591 checks) and documentation links passed; the optional modern-comment
fixture compiled only. No desktop input, foreground activation, document text
read or Word/Excel document edit was used in this pass. New live Word/Excel
keypress latency was not measured.

The application-only development update was activated from temporary
`out/word-comments/resident-navigation-excel-1` with the existing installed
profile. All **41 protected installed file hashes** and startup registration
were unchanged. Released 1.9.6, its installed executable and accepted baseline
remain unchanged; no installer was built and nothing was published. Evidence is
under `out/word-navigation-excel`: targeted results, read-only shortcut reports
and a copy of the activation record.

## Excel multiline cells and live capability review — 26 October 2026

The user authorized the next Excel pass without desktop takeover. Read-only
capability probes inspected existing window/editor metadata before and after the
user hid the Formula Bar in a disposable edited cell. The visible logical
`FormulaBar` exposed TextPattern; the hidden state removed that provider. Neither
native editor supplied a usable text/selection interface. Standard Edit messages
returned unusable values and were not adopted as a production fallback. No cell
text was read by the probes, and no workbook, foreground or display setting was
changed by them. The Formula Bar requirement remains for this Excel version.

The development adapter now supports words/letters and selected single-line
regions within multiline cells. Relative Left/Right navigation starts from a
verified selection/caret, maps LF/CRLF boundaries and never issues line-local
Home/End or spreadsheet Ctrl commands on that path. It rejects unproven cursor
clusters before input. Selected passages spanning line breaks still skip because
ordinary Unicode typing is not a verified line-break insertion transport. Existing
formula and size limits remain. Navigation and replacement events use the
foreground-owned correction tag; fresh text/range/mode and epoch checks remain.

Word discovery now resolves selection/caret from one captured Excel buffer;
clipboard discovery resolves selection/insertion from one capture. Replacement
uses EnsureEdit's existing text/range proof rather than repeating that read
immediately before it, and retains the final pre-input proof. The real adapter's
transaction methods are exercised through an independent field backend, without
native input. The new fixture covers 49,825 assertions, including relative
selection, both line endings, retained selection, continuation, punctuation,
restoration, stale ownership, ignored selection, unknown modes and formula output.

All **14 targeted suites passed, 1,660,035 assertions; zero failures**.
ExcelCellTests passed **52,045 assertions**. A final Excel rerun passed after the
backend declaration was changed to an auto-property to remove an unused-field
compiler warning. All **19 compiled-app checks**, **100 C# formatting checks**,
development audit (1,612 checks) and documentation links passed. The capability
tool compiled separately and is compile-only by default; `-ReadOnly` explicitly
enables its bounded metadata inspection.

After activation, the user performed word/letter checks and second-line conversion
in a disposable Excel cell and confirmed all worked normally. Passive diagnostic
timings contained seven visible-bar manual attempts between **99.1 and 362.2 ms**
(median **200.7 ms**). Six involved abbreviation expansion and one was ordinary
word conversion (**260.2 ms**), so these do not establish a controlled word/letter
speedup or the 150 ms goal. One rejected range read occurred during the observed
successful checks and is retained as a transient read observation. Three earlier
hidden-bar attempts were correctly skipped and are excluded from conversion
timings. Timing boundaries are accepted manual action to empty-hold release;
they are not independent hardware key timestamps or queued-typing delivery.

The temporary application-only build is running from
`out/word-comments/resident-excel-broader-1` with the existing installed profile.
Activation verified all **41 protected installed file hashes** and unchanged
startup registration. The agent did not move focus, inject test input, open/edit
a working workbook or change Word shortcuts/formatting. The user operated the
scratch cell and Formula Bar. Release 1.9.6, the installed executable and accepted
baseline remain unchanged. No installer/add-in was built or installed, and
nothing was published.

Evidence is under `out/excel-broader-support` (targeted and final Excel results,
passive timing metadata and activation record) and `out/excel-capabilities`
(visible/hidden metadata reports).

## Excel passages, complete regression and persistent app update — 26 October 2026

The earlier temporary activations above are superseded by this app-only update.
The verified development executable now runs from the normal installed path,
`%APPDATA%\KeyJaya\engine\keyjaya-app.exe`. Its default installed-profile resolution
was verified without a `--root` argument. The Windows startup command was not
changed and now resolves to this binary. The executable version and accepted
release baseline remain 1.9.6; this is an unpublished development update.

Excel relative selection now chooses the nearest verified selection edge even
in single-line cells. Cursor-unit validation covers the selected span and actual
travel path, allowing emoji/combining sequences elsewhere to remain untouched.
The established single-line script/cluster fallback remains. Selected LF/CRLF
passages use balanced Alt+Enter insertion, with preflight for both insertion and
retained selection. Current buffer/range/mode, formula, size, epoch and native
ownership guards remain. Status-mode metadata uses a fresh batched UIA request.
No clipboard transport was introduced.

The user operated a disposable Excel cell and confirmed conversion across two
lines, retained selection, one-step Ctrl+Z, and a nearby word with emoji or a
combining accent elsewhere all worked. The agent did not inject test input,
move foreground focus or open/read/edit working documents. Eight mixed passive
action timings ranged from 12.0 to 874.7 ms; they include selection/undo and
provider retries and are not a controlled conversion-speed comparison. The
Formula Bar and English mode-label requirements remain, as do exclusions for
formula-like/oversized buffers and unverified multiline cursor clusters.

Verification completed:

- All **44 noninteractive suites passed: 26,997,690 assertions, zero failures**.
  ExcelCellTests passed **53,101**, including **50,881** transaction-contract
  assertions. WordSnapshotTests passed **601**. The shared editor matrix still
  covers six profiles and nine scenarios without controlling those applications.
- All **26 Python developer-tool tests** passed; network and installer calls in
  those fixtures are mocked.
- The app-only build and all **19 compiled-app checks** passed. No installer or
  payload package was built.
- All **100 C# formatting checks**, the **1,612-check development audit**, pinned
  Hindi-source verification, documentation checks and `git diff --check` passed.
  Intentional development drift remains separate from the accepted release data.
- All **17 engine benchmark cases** passed with 301 warm samples per case and no
  budget failures. Highest warm p95: **11.293 ms**; initialization: **209.905 ms**.
  These do not measure native editor latency. A long-cell navigation fixture
  independently proves **10 events instead of 3,614** for the same nearby word.
- A disposable binary-update fixture passed **25 checks**, including successful
  replacement, locked-target refusal, hash mismatch and rollback after an actual
  swap followed by injected failure. No live profile was used by that fixture.

The live app-only update gracefully closed only the verified owned candidate,
atomically replaced the installed executable, and launched it hidden with its
default profile. The original executable and unchanged framework configuration
were backed up beneath `out/stability-next/installed-backup-*`. All **47 protected
profile files** (configuration, custom triggers, dictionaries/assets, retained
state and framework configuration) had identical before/after SHA-256 hashes.
Startup registration was unchanged. Tray Restart resolves the running executable
path, so subsequent launches use the installed development binary as well.

Evidence is under `out/stability-next`: `full44-results.json`,
`binary-transaction-tests.json`, `app-only-update.json`, passive timing metadata
and the original binary backup. The installed SHA-256 is
`4DC74DB230D890467A861549D897124DF775F534E5460290E8D4F08C14535B47`.
The engine report is beneath
`out/benchmarks/run-127d885b16a74ae58d07e7d16e6ce13d`. No installer/add-in was built
or installed, no Word shortcuts or templates were changed, and nothing was
published.

## Spelling editor and read-only Word shortcut inspection — 26 October 2026

The tray now includes **Edit spellings and style…** and **Check Word shortcut
conflicts…**. The spelling editor manages preferred Unicode spellings, protected
words and roman/italic/name exceptions using the existing configuration policy.
It shows legacy rules without rewriting legacy files and saves only personal
word/style overrides in `customizations.ini`. Untouched sections, comments, line
endings and BOM are preserved. Save creates an exact backup, refuses changed
snapshots, commits atomically and rolls back failed reloads while retaining newer
external edits. No test changed the user's actual rules or triggers.

The Word check is also available inside Customize shortcuts, where it can inspect
proposed bindings without saving them. It reads only shortcut and active-window
ownership metadata from already-open Word instances. A separate hidden helper
keeps Word COM work outside the resident; a five-second parent deadline stops
only that helper if it stalls. It does not activate Word, read document text,
set CustomizationContext, execute a Word command, change assignments or save a
template. Reports distinguish KeyJaya overlaps, changed navigation assignments,
Word shortcut prefixes, unassigned keys and unavailable/incomplete checks.

The native read-only check found one accessible Word instance. All six checked
navigation chords were normal, including **Ctrl+Shift+Left → WordLeftExtend**.
Five KeyJaya/Word overlaps were reported: Ctrl+Comma and Ctrl+Slash were Word
shortcut prefixes; Ctrl+Backslash was ToggleMasterSubdocs; Ctrl+LeftBracket and
Ctrl+RightBracket were ShrinkFontOnePoint and GrowFontOnePoint. These existing
assignments were not changed. A report applies to each instance's active window,
rather than every open document/template.

Verification completed:

- All **44 isolated suites passed: 26,997,748 assertions, zero failures**.
- Final focused checks after the editor input-guard change passed **seven suites,
  1,817,271 assertions**. Spelling-editor contracts passed **44** assertions and
  shortcut-inspection contracts **26**, including production-parent helper startup,
  malformed output and termination after a simulated five-second stall. Helpers
  in these tests never connect to Word.
- Resident-owned editor windows and message boxes now bypass conversion by
  process ownership. Both keyboard decisions and registered-shortcut dispatch
  use that guard. The separate cheat-sheet exception remains. This prevents
  automatic or manual conversion from changing text being entered into settings.
- Five forms were rendered privately without displaying any windows, at 96, 144
  and 192 DPI: **15 renders and 132 visibility/layout checks**. A clipped footer
  at 200% scaling was corrected. Offscreen printing does not fully paint native
  combo boxes; their standard controls and selection state remain separate from
  this visual evidence. No desktop screenshot or input automation was used.
- The installed spelling configuration loaded successfully in a read-only
  preflight. All **26 Python tests**, **19 final compiled-app checks**, **106 C#
  formatting checks**, **1,658 development-audit checks** and pinned Hindi-source
  verification passed. All **17 engine benchmark cases** passed, with no budget
  failures. Engine measurements remain separate from native editor response time.

Evidence is under `out/spelling-word`: `full44-results.json`,
`guard-focused-results.json`, `word-inspection.json`, private render PNGs and
app-only update records/backups. The engine report is under
`out/benchmarks/run-ee9726a608eb431d8084e35f92f34c1f`.

After the user confirmed readiness, the final build, including the editor-input
guard, was activated at the normal installed/startup executable path. The restart
verified an idle, stable chat/desktop foreground, released buttons/modifiers,
resident ownership and absence of visible KeyJaya settings windows. The chat host
was verified by its Codex product identity; this packaged installation's executable
is named ChatGPT.exe. No foreground activation or desktop input was sent.

The previous app was backed up, only the executable was replaced, and the new
resident's default profile was verified without `--root`. All **47 protected
profile files** and Windows startup registration remained unchanged. A separate
final read-only verification confirmed the running executable matches the tested
source, verified its backup, and found all 47 hashes still unchanged. The final
SHA-256 is `BEFBF6CCB015930D4BDD52504E4CACC672ED9CF30ABB7EFC620432060858CB55`.
Activation and final checks are in `app-only-update.json` and
`final-verification.json` beneath `out/spelling-word`. Working documents were not
opened/read/edited by these checks. No installer was built or run, nothing was
published, and the accepted 1.9.6 release baseline remains unchanged.

## Ctrl-word italics and requested menu cleanup — 26 October 2026

The spelling editor has been removed, including its store/window and compilation
inputs. Existing configuration and spelling/style preferences remain supported
and were not rewritten or removed. The standalone Word shortcut-overlap tray
action is also removed; its read-only check remains inside Customize shortcuts.
The menu fixture and regenerated menu image reflect these changes.

The owner defined the new formatting policy before implementation:

- An already-correct recognized Sanskrit/Bengali spelling, including `bhakti`,
  `nitya`, `dvija` and `dvijas`, is italicized unchanged on the first Ctrl tap.
- Individual terms and loanwords can be italic, including `dharma`, `guru`,
  `yoga`, `karma` and their plurals. One selected word counts as an individual
  word conversion. Ordinary English and unrecognized words remain protected.
- Selected passages italicize eligible terms but actively make the regular-type
  loanword exceptions regular, even if they were previously italic. Names keep
  their regular-type policy; book-title and personal override rules remain.
- Whole Sanskrit compounds such as `bhakti-yoga`, `nitya-dharma` and `guru-sevā`
  are treated as one italic term, including in passages.
- Repeated word taps cycle alternative spellings and the original text/format.
  A word without alternatives cycles between italic and its original formatting.
- The existing automatic-italics setting controls the behavior. Word comments,
  plain text, text-only Excel editing and script outputs gain no unsupported
  formatting. Existing supported rich-text/Markdown formatting paths are used.

The production Ctrl-word plan records an explicit original-formatting position,
so the same text can appear once as italic and once with its original formatting.
Word retains native font snapshots, expected-text/range checks and its undo
record; repeated cycles reuse the original continuation style. A single-word
selection can share the word path while retaining selection. Explicit regular
spans clear italics without changing unrelated text. InDesign preserves font
weight when choosing a nonitalic face; rich HTML emits normal-style spans.
Markdown handles owned emphasis markers while preserving bold and protected
code/links/headings. Wrapper expansion verifies provider text before selection.

Verification completed:

- All **44 isolated suites passed: 26,998,329 assertions, zero failures**.
- Final book-title/personal-rule review was followed by **five focused suites,
  647,496 assertions**, including **606 Ctrl-italics contract assertions**.
  Mixed-font passage checks include both italic and regular spans; the shared
  editor matrix, Word ownership/font lifetime, navigation and replay checks pass.
- All **26 Python developer-tool tests**, **19 compiled-app checks**, **105 C#
  formatting checks**, **1,650 development-audit checks**, pinned source checks,
  documentation links and `git diff --check` passed.
- All **17 engine benchmark cases** passed with 301 warm samples and no budget
  failures. Highest warm p95: **4.264 ms**; initialization: **206.74 ms**. These
  measure the engine, not native editor timing or the entire new Ctrl workflow.

The candidate is running from the normal installed/startup executable path,
using the default installed profile. Its previous executable was backed up.
All **47 protected profile files** and startup registration were unchanged during
activation and separate final read-only verification. The executable SHA-256 is
`FAAE60DA009A6038F555006A7400E74BF248E9356942C6C82E419DDCE0E83FD4`.
The owner reported that everything in the scratch Word check worked, including
the requested formatting cycle and passage undo, but that both individual words
and passages were slower than desired. This is user-observed native behavior;
the offline models alone do not prove visible formatting or undo. The agent sent
no test input and did not open/read/edit working documents. No installer was built or
run, nothing was published, and the accepted 1.9.6 release baseline is unchanged.

Evidence is beneath `out/ctrl-italics`: `agreed-policy.json`, `full44-results.json`,
`final-focused-results.json`, `compiled-checks.json`, app-only update/verification
records and the prior binary backup. The engine report is under
`out/benchmarks/run-6c8985685fe84c71a7724b8c4c81d71c`.

## Word body latency follow-up — 26 October 2026

After confirming the new Ctrl formatting behavior, the owner reported excessive
delay for both individual words and selected passages. Read-only diagnostics for
the initial candidate showed ten Word word actions with an accepted-action to
empty-input-hold-release median of **771.8 ms**, range **616.3–3,411.1 ms**. Two
selected-passage actions were **1,735.3 ms** and **2,089.5 ms**. These are sparse,
uncontrolled samples; empty releases can also occur on skipped work. The summary
includes only actions that entered native formatting with no logged rejection;
the owner's visible-result observation remains separate.

The follow-up removes duplicate work while retaining edit ownership:

- Word Ctrl discovery reuses its own completed paste-UI preparation and selection
  observation within the same owner/generation. Final focus, caret/selection,
  expected-text and native commit checks remain.
- Replay ownership is captured inside the native edit from its verified document
  and visible projection, removing a second Word binding and revision scan.
  An owned font snapshot can supply the original continuation style directly.
- Original-font restoration uses the transaction's verified result projection,
  preserving character fonts and tracked-deletion coordinates. Live native
  styles already matching an italic/regular request avoid an unnecessary write.

Six focused suites passed **647,569 assertions**, including **29** new production
replay/font/style contracts, the selection matrix, formatting rules, shortcut
inspector, navigation and ordered replay checks. The fake range rejects duplicate
text/revision reads and counts unchanged style writes; ownership, cancellation,
provider failures and one-record undo are exercised without Word or desktop input.
All **19 compiled-app checks**, **106 C# formatting checks**, **1,657 development
audit checks**, **39 local documentation links**, **4 image headers** and
`git diff --check` passed. The earlier complete 44-suite run is recorded above;
the follow-up used focused checks appropriate to these adapter changes.

The app-only candidate was activated from the normal installed/startup path.
Separate read-only verification confirmed the running binary, backup and default
profile. All **47 protected profile files** and startup registration were unchanged.
The SHA-256 is `41518FB5ACBF01B462E985A102118B132C6F412E2AACF7DEDC8826303C34E454`.
The owner confirmed **“Faster, and all still works”** for individual words,
passages, formatting and one-step undo in a scratch Word document. The seven
accepted word actions in the follow-up diagnostics had a median of **588.8 ms**,
range **561.1–1,132.6 ms**. Two passage actions were **1,075.0 ms** and
**1,088.5 ms**. These are uncontrolled samples with different action histories;
they are not a controlled percentage-speedup claim. No agent desktop input or
working-document access was used. No installer was built/run,
nothing was published and the accepted 1.9.6 release baseline is unchanged.

The final review retains fresh selection discovery after recovering a paste
popup that had hidden the selection provider. Positive pre-popup selection
evidence still requires an exact match. This last guard passed Word snapshot and
ordered-replay checks again: **48,520 assertions**, plus the compiled-app probe.
Its tested binary is `84A0324A0A5629A883F4C88EF8A3C0D189719BB5F2065D2FA590CDE4BB31EABE`.
The guarded restart initially detected desktop activity and left the first
faster build running. After the owner confirmed idle-input readiness, the final
guard was activated at the normal installed/startup path. Separate read-only
verification confirmed the running executable matches that tested SHA-256,
the previous executable backup is valid, and all **47 protected profile files**
and startup registration are unchanged. No desktop input was sent, no working
document was accessed, no installer was built/run and nothing was published.

The owner then confirmed that the final scratch check passed: paste `bhakti`,
tap Ctrl immediately, and press Ctrl+Z once. This confirms visible italics and
native undo after pasting on the final activated build, separately from the
automated snapshot/replay models. The result is recorded in
`out/ctrl-italics-speed/manual-word-after-paste-check.json`.

Evidence is beneath `out/ctrl-italics-speed`: `focused-results.json`,
`compiled-checks.json`, `baseline-timings.json`, `candidate-timings.json`,
`manual-word-check.json`, guarded activation/final-verification records and the
prior executable backup. The timing summarizer emits only phase
timings and generic action metadata, without document text.

## Four review findings resolved — 27 October 2026

The review of checkpoint `da9c8df` identified four actionable formatting/undo
defects. Each is addressed in the development candidate:

- Markdown emphasis removal validates opening/closing delimiter boundaries and
  escape parity, including context immediately outside a selection. Literal
  identifiers such as `note_dharma_id` retain their underscores. Reconstruction
  preserves bold and avoids stray emphasis markers beside punctuation.
- Native Word font capture accepts the same bounded one-word shapes as Ctrl
  classification, including surrounding whitespace/punctuation, up to 321
  characters. Original fonts remain owned while the Ctrl cycle is active, even
  after the separate Backspace deadline. A missing snapshot cannot silently
  commit an original-formatting state. Ordinary invalidation still retires
  references without blocking navigation.
- Formatting-only conversions arm manual Backspace undo. The saved original
  anchor passes explicitly into restoration after the current anchor is cleared;
  supported rich editors restore the original italic payload and InDesign uses
  its native snapshot. Restoration does not run automatic style policy again.
  Modern Word comments retain their text-only restoration path.
- Both positive and regular-type policy respect the established protected
  literal ranges. URLs, email addresses, paths and literal comments receive no
  new spans; eligible terms on either side still follow normal policy.

Verification completed:

- All **44 isolated suites passed: 26,998,434 assertions, zero failures**.
- Final Markdown selection-boundary refinements passed **five focused suites,
  647,601 assertions**, including **660 Ctrl-italics assertions** and **22 native
  font-cycle/undo assertions**. The latter call production capture, restoration,
  expiry and undo-decision code against fake ranges; they preserve differing
  font names, sizes, bold and italic states for each captured character.
- The punctuation regression was observed failing before its correction. The
  earlier review's production probes record the identifier corruption,
  protected-range spans and missing formatting evidence before these fixes.
- All **26 Python developer-tool tests**, **19 compiled-app checks**, **107 C#
  formatting checks**, **1,671 development-audit checks**, pinned Hindi-source
  checks, **42 local documentation links**, **4 image headers** and
  `git diff --check` passed. Python installer/network operations were mocked.
- All **17 engine benchmarks**, with 301 warm samples each, passed their budgets.
  Highest warm p95: **4.490 ms**; initialization: **219.880 ms**. These are engine
  measurements, not native editor response measurements.

The app-only executable passed the compiled probe. KeyJaya was already closed;
only its installed executable was atomically replaced, preserving that state.
The previous executable was backed up and verified. Separate read-only checks
confirmed the installed executable matches the tested candidate, all **47
protected profile files** and startup registration are unchanged, and no app was
started or stopped. The SHA-256 is
`8DBFDCDA574554A30B974A7AEB29AB739041648F7C888779DCEF069A7163AFA1`.

No agent desktop input or working-document access was used. The new native
interaction cases have automated model/helper coverage; visible browser/InDesign
Backspace and the padded/delayed Word cases were not physically exercised in this
pass. The earlier user confirmations apply to the preceding build. No installer
was built or run, nothing was pushed/published, and version/release baseline 1.9.6
remains unchanged.

Evidence is beneath `out/review-fixes`: `full44-results.json`,
`final-focused-results.json`, `compiled-checks.json`, guarded stopped-app update,
backup and final-verification records. Earlier reproduction output is beneath
`out/reviews/checkpoint-da9c8df`. The engine report is under
`out/benchmarks/run-31fedea549dd49129713434757ed3b0e`.
